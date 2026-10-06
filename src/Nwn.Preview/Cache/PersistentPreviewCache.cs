using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace Nwn.Preview.Cache;

/// <summary>Stores regenerable preview bytes under a caller-supplied content fingerprint.</summary>
public sealed class PersistentPreviewCache
{
    private static readonly byte[] Magic = "NWNPV001"u8.ToArray();
    private static readonly ConcurrentDictionary<string, object> BudgetGates = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();
    private readonly string _root;
    private readonly string _renderVersion;
    private readonly long _maximumBytes;
    private readonly string _budgetRoot;

    public PersistentPreviewCache(string root, string renderVersion, long maximumBytes, string? budgetRoot = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        ArgumentException.ThrowIfNullOrWhiteSpace(renderVersion);
        if (maximumBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximumBytes));
        _root = Path.GetFullPath(root);
        _renderVersion = renderVersion;
        _maximumBytes = maximumBytes;
        _budgetRoot = Path.GetFullPath(budgetRoot ?? root);
    }

    public string RootPath => _root;

    public bool Contains(string key, string fingerprint)
    {
        ValidateArguments(key, fingerprint);
        if (HasReparsePointInPath(_root))
            return false;
        var path = EntryPath(key);
        lock (_gate)
        {
            var corrupt = false;
            var valid = false;
            try
            {
                if (!File.Exists(path) || IsReparsePoint(path))
                    return false;
                using (var stream = File.OpenRead(path))
                using (var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true))
                {
                    if (!reader.ReadBytes(Magic.Length).AsSpan().SequenceEqual(Magic))
                        corrupt = true;
                    else if (!string.Equals(reader.ReadString(), EffectiveFingerprint(fingerprint), StringComparison.Ordinal))
                        return false;
                    else
                    {
                        _ = reader.ReadBoolean();
                        var expectedHash = reader.ReadBytes(32);
                        var length = reader.ReadInt32();
                        corrupt = expectedHash.Length != 32 || length < 0 || length > _maximumBytes ||
                                  stream.Length - stream.Position != length;
                        if (!corrupt)
                            valid = HasMatchingPayloadHash(stream, expectedHash, length);
                        corrupt |= !valid;
                    }
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
            {
                corrupt = true;
            }

            if (corrupt)
                return RemoveCorrupt(path);
            if (!valid)
                return false;
            try { File.SetLastAccessTimeUtc(path, DateTime.UtcNow); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            return true;
        }
    }
    public DateTime? LastWrittenUtc(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        try
        {
            if (HasReparsePointInPath(_root))
                return null;
            var path = EntryPath(key);
            return File.Exists(path) && !IsReparsePoint(path) ? File.GetLastWriteTimeUtc(path) : null;
        }
        catch (IOException) { return null; }
        catch (UnauthorizedAccessException) { return null; }
    }

    public bool TryRead(string key, string fingerprint, out byte[]? payload, out bool hasImage)
    {
        ValidateArguments(key, fingerprint);
        payload = null;
        hasImage = false;
        if (HasReparsePointInPath(_root))
            return false;
        var path = EntryPath(key);
        lock (_gate)
        {
            try
            {
                if (!File.Exists(path) || IsReparsePoint(path))
                    return false;
                using var stream = File.OpenRead(path);
                using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
                if (!reader.ReadBytes(Magic.Length).AsSpan().SequenceEqual(Magic))
                    return RemoveCorrupt(path);
                if (!string.Equals(reader.ReadString(), EffectiveFingerprint(fingerprint), StringComparison.Ordinal))
                    return false;
                var storedImage = reader.ReadBoolean();
                var expectedHash = reader.ReadBytes(32);
                var length = reader.ReadInt32();
                if (expectedHash.Length != 32 || length < 0 || length > _maximumBytes ||
                    stream.Length - stream.Position != length)
                    return RemoveCorrupt(path);
                payload = reader.ReadBytes(length);
                if (payload.Length != length ||
                    !CryptographicOperations.FixedTimeEquals(expectedHash, SHA256.HashData(payload)))
                    return RemoveCorrupt(path);
                hasImage = storedImage;
                File.SetLastAccessTimeUtc(path, DateTime.UtcNow);
                return true;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
            {
                RemoveCorrupt(path);
                payload = null;
                hasImage = false;
                return false;
            }
        }
    }

    public void Write(string key, string fingerprint, ReadOnlySpan<byte> payload, bool hasImage)
    {
        ValidateArguments(key, fingerprint);
        if (payload.Length > _maximumBytes)
            return;
        if (HasReparsePointInPath(_root) || HasReparsePointInPath(_budgetRoot))
            return;
        var path = EntryPath(key);
        lock (_gate)
        {
            string? temporary = null;
            try
            {
                EnsureOwnedDirectories();
                if (File.Exists(path) && IsReparsePoint(path))
                    File.Delete(path);
                temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
                {
                    writer.Write(Magic);
                    writer.Write(EffectiveFingerprint(fingerprint));
                    writer.Write(hasImage);
                    writer.Write(SHA256.HashData(payload));
                    writer.Write(payload.Length);
                    writer.Write(payload);
                    writer.Flush();
                    stream.Flush(flushToDisk: true);
                }
                File.Move(temporary, path, overwrite: true);
                temporary = null;
                EnforceBudget(path);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // A cache write failure only costs a later re-render.
            }
            finally
            {
                if (temporary is not null)
                    TryDelete(temporary);
            }
        }
    }

    public void Remove(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        lock (_gate)
        {
            if (!HasReparsePointInPath(_root))
                TryDelete(EntryPath(key));
        }
    }

    public int Clear()
    {
        lock (_gate)
        {
            var removed = 0;
            foreach (var path in EnumerateSafeEntries(_root))
            {
                try { File.Delete(path); removed++; }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
            return removed;
        }
    }

    private void EnsureOwnedDirectories()
    {
        EnsureSafeDirectory(_budgetRoot);
        EnsureSafeDirectory(_root);
    }

    private static void EnsureSafeDirectory(string path)
    {
        var current = Path.GetPathRoot(path) ?? throw new IOException("The preview cache path has no root.");
        if (Directory.Exists(current) && IsReparsePoint(current))
            throw new IOException("Preview cache paths cannot traverse reparse points.");

        foreach (var segment in path[current.Length..].Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                     StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);
            if (!Directory.Exists(current))
                Directory.CreateDirectory(current);
            if (IsReparsePoint(current))
                throw new IOException("Preview cache paths cannot traverse reparse points.");
        }
    }

    private string EffectiveFingerprint(string fingerprint) => _renderVersion + "\n" + fingerprint;

    private string EntryPath(string key)
    {
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return Path.Combine(_root, Convert.ToHexStringLower(digest) + ".bin");
    }

    private static bool HasMatchingPayloadHash(Stream stream, byte[] expectedHash, int length)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> buffer = stackalloc byte[64 * 1024];
        var remaining = length;
        while (remaining > 0)
        {
            var read = stream.Read(buffer[..Math.Min(buffer.Length, remaining)]);
            if (read == 0)
                return false;
            hash.AppendData(buffer[..read]);
            remaining -= read;
        }
        var actualHash = hash.GetHashAndReset();
        return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
    }

    private bool RemoveCorrupt(string path)
    {
        TryDelete(path);
        return false;
    }

    private void EnforceBudget(string justWritten)
    {
        lock (BudgetGates.GetOrAdd(_budgetRoot, static _ => new object()))
        {
            using var budgetMutex = new Mutex(initiallyOwned: false, BudgetMutexName(_budgetRoot));
            var ownsBudget = false;
            try
            {
                try { ownsBudget = budgetMutex.WaitOne(TimeSpan.FromSeconds(5)); }
                catch (AbandonedMutexException) { ownsBudget = true; }
                if (!ownsBudget || !Directory.Exists(_budgetRoot) || HasReparsePointInPath(_budgetRoot) || HasReparsePointInPath(_root))
                    return;

                var files = EnumerateSafeEntries(_budgetRoot).Select(path => new FileInfo(path)).ToArray();
                long total = files.Sum(file => file.Length);
                foreach (var file in files.OrderBy(file => string.Equals(file.FullName, justWritten, StringComparison.OrdinalIgnoreCase))
                             .ThenBy(file => file.LastAccessTimeUtc))
                {
                    if (total <= _maximumBytes)
                        break;
                    try { total -= file.Length; file.Delete(); }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
            }
            finally
            {
                if (ownsBudget)
                    budgetMutex.ReleaseMutex();
            }
        }
    }

    private static string BudgetMutexName(string budgetRoot)
    {
        var identity = OperatingSystem.IsWindows() ? budgetRoot.ToUpperInvariant() : budgetRoot;
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(identity));
        return "NwnPreviewCacheBudget_" + Convert.ToHexString(digest);
    }

    private static string[] EnumerateSafeEntries(string root)
    {
        if (!Directory.Exists(root) || HasReparsePointInPath(root))
            return [];
        var result = new List<string>();
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var directory = pending.Pop();
            try
            {
                foreach (var file in Directory.EnumerateFiles(directory, "*.bin", SearchOption.TopDirectoryOnly))
                    if (!IsReparsePoint(file)) result.Add(file);
                foreach (var child in Directory.EnumerateDirectories(directory, "*", SearchOption.TopDirectoryOnly))
                    if (!IsReparsePoint(child)) pending.Push(child);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
        return result.ToArray();
    }

    private static bool HasReparsePointInPath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var current = Path.GetPathRoot(fullPath);
        if (current is null || IsReparsePoint(current))
            return true;

        foreach (var segment in fullPath[current.Length..].Split(
                     [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);
            if (Directory.Exists(current) && IsReparsePoint(current))
                return true;
        }
        return false;
    }
    private static bool IsReparsePoint(string path)
    {
        try { return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0; }
        catch (FileNotFoundException) { return false; }
        catch (DirectoryNotFoundException) { return false; }
        catch (IOException) { return true; }
        catch (UnauthorizedAccessException) { return true; }
    }

    private static void ValidateArguments(string key, string fingerprint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(fingerprint);
        if (fingerprint.Length > 256)
            throw new ArgumentOutOfRangeException(nameof(fingerprint), "Use a compact content fingerprint.");
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
