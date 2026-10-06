// SPDX-License-Identifier: MIT

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace Nwn.Formats.Io;


/// <summary>An exclusive, cross-process lease for mutations beneath one unpacked module root.</summary>
/// <remarks>
/// Desktop tools and packers are separate processes, so a file lease prevents a pack or unpack
/// from walking a module while a writer replaces its resources. Its persistent empty file is keyed
/// by normalized module path; exclusivity comes from the open handle, not file existence.
/// </remarks>
public sealed class ModuleWriteLock : IDisposable
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(25);
    private static readonly AsyncLocal<Dictionary<string, HeldLease>?> AmbientLeases = new();
    private static readonly HashSet<string> ResourceDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        "are", "dlg", "fac", "gic", "git", "ifo", "itp", "jrl", "ncs", "nss",
        "utc", "utd", "ute", "uti", "utm", "utp", "uts", "utt", "utw"
    };

    private readonly string _moduleKey;
    private HeldLease? _held;

    private ModuleWriteLock(string moduleKey, HeldLease held)
    {
        _moduleKey = moduleKey;
        _held = held;
    }

    /// <summary>Acquires the module exclusively, waiting for the current writer to finish.</summary>
    public static ModuleWriteLock Acquire(string moduleRoot, TimeSpan? timeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleRoot);
        var moduleKey = NormalizeModuleRoot(moduleRoot);
        var ambient = AmbientLeases.Value;
        if (ambient != null && ambient.TryGetValue(moduleKey, out var existing) && existing.TryRetain())
            return new ModuleWriteLock(moduleKey, existing);

        var lockDirectory = Path.Combine(Path.GetTempPath(), "Nwn.ModuleLocks");
        Directory.CreateDirectory(lockDirectory);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(moduleKey)));
        var lockPath = Path.Combine(lockDirectory, hash + ".lock");
        var wait = timeout ?? DefaultTimeout;
        var stopwatch = Stopwatch.StartNew();
        Exception? lastFailure = null;
        FileStream? stream = null;
        do
        {
            try
            {
                stream = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite,
                    FileShare.None, bufferSize: 1, FileOptions.None);
                break;
            }
            catch (IOException ex) { lastFailure = ex; }
            catch (UnauthorizedAccessException ex) { lastFailure = ex; }
            if (stopwatch.Elapsed < wait)
                Thread.Sleep(RetryDelay);
        } while (stopwatch.Elapsed < wait);

        if (stream == null)
            throw new ModuleWriteLockException(moduleKey, lastFailure!);

        var held = new HeldLease(stream);
        var updated = ambient == null
            ? new Dictionary<string, HeldLease>(PathComparer)
            : new Dictionary<string, HeldLease>(ambient, PathComparer);
        updated[moduleKey] = held;
        AmbientLeases.Value = updated;
        return new ModuleWriteLock(moduleKey, held);
    }

    /// <summary>Acquires the module containing a loose resource path.</summary>
    public static ModuleWriteLock AcquireForResourcePath(string resourcePath, TimeSpan? timeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourcePath);
        var fullPath = Path.GetFullPath(resourcePath);
        var directory = Directory.Exists(fullPath)
            ? fullPath
            : Path.GetDirectoryName(fullPath)
              ?? throw new InvalidOperationException($"Could not determine the containing directory of '{resourcePath}'.");
        var directoryName = Path.GetFileName(directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var moduleRoot = ResourceDirectories.Contains(directoryName)
            ? Directory.GetParent(directory)?.FullName
              ?? throw new InvalidOperationException($"Could not determine the module root containing '{resourcePath}'.")
            : directory;
        return Acquire(moduleRoot, timeout);
    }

    public void Dispose()
    {
        var held = Interlocked.Exchange(ref _held, null);
        if (held == null || !held.Release())
            return;
        var ambient = AmbientLeases.Value;
        if (ambient != null && ambient.TryGetValue(_moduleKey, out var current) && ReferenceEquals(current, held))
        {
            var updated = new Dictionary<string, HeldLease>(ambient, PathComparer);
            updated.Remove(_moduleKey);
            AmbientLeases.Value = updated.Count == 0 ? null : updated;
        }
        held.Stream.Dispose();
    }

    private static string NormalizeModuleRoot(string moduleRoot)
    {
        var normalized = Path.GetFullPath(moduleRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return OperatingSystem.IsWindows() ? normalized.ToUpperInvariant() : normalized;
    }

    private static StringComparer PathComparer => OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    private sealed class HeldLease(FileStream stream)
    {
        private int _depth = 1;
        public FileStream Stream { get; } = stream;
        public bool TryRetain()
        {
            while (true)
            {
                var depth = Volatile.Read(ref _depth);
                if (depth <= 0) return false;
                if (Interlocked.CompareExchange(ref _depth, depth + 1, depth) == depth) return true;
            }
        }
        public bool Release() => Interlocked.Decrement(ref _depth) == 0;
    }
}
