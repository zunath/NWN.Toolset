using System.Security.Cryptography;
using System.Text.Json;

namespace Nwn.Authoring.Editing;

/// <summary>Commits and recovers guarded file removals and replacements.</summary>
/// <remarks>
/// Callers must hold their established cross-process mutation leases across commit and recovery.
/// This class is lock-neutral so a host can preserve its required ordering across multiple roots.
/// </remarks>
public static class FileTransaction
{
    private const int ManifestVersion = 1;
    private const int MaximumManifestBytes = 256 * 1024 * 1024;
    private const string ManifestSuffix = ".file-transaction.json";
    private const string BackupSuffix = ".file-delete-backup";
    private static readonly JsonSerializerOptions ManifestOptions = new() { MaxDepth = 32 };

    /// <summary>
    /// Revalidates the captured generations, commits guarded replacements, then removes the
    /// captured files. The manifest is the rollback record until every mutation has completed.
    /// </summary>
    public static FileTransactionResult Commit(FileTransactionPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        plan.VerifyCurrentGeneration();
        var transactionId = Guid.NewGuid().ToString("N");
        var entries = CreateManifestEntries(plan, transactionId);
        var mutationPaths = new HashSet<string>(plan.AffectedPaths, FileTransactionPlan.PathComparer);
        foreach (var entry in entries.Where(entry => entry.Kind == TransactionEntryKind.Delete))
        {
            if (!mutationPaths.Add(entry.BackupPath!))
                throw new IOException("A transaction backup aliases a planned mutation target.");
            FileTransactionPlan.EnsureNoReparsePoint(entry.BackupPath!,
                FileTransactionPlan.FindContainingRoot(plan.AllowedRootsUnsafe, entry.BackupPath!));
            if (File.Exists(entry.BackupPath) || Directory.Exists(entry.BackupPath!))
                throw new IOException($"A transaction backup already exists: {entry.BackupPath}");
        }

        var manifest = new TransactionManifest
        {
            Version = ManifestVersion,
            TransactionId = transactionId,
            TransactionRoot = plan.TransactionRoot,
            AllowedRoots = plan.AllowedRootsUnsafe.ToList(),
            Entries = entries
        };
        var manifestPath = ManifestPath(plan.TransactionRoot, transactionId);
        if (!mutationPaths.Add(manifestPath))
            throw new IOException("The transaction manifest aliases a planned mutation target.");
        FileTransactionPlan.EnsureNoReparsePoint(manifestPath, plan.TransactionRoot);
        if (File.Exists(manifestPath) || Directory.Exists(manifestPath))
            throw new IOException("The transaction manifest path already exists.");

        var manifestWritten = false;
        try
        {
            WriteManifest(manifestPath, manifest);
            manifestWritten = true;

            foreach (var entry in entries.Where(entry => entry.Kind == TransactionEntryKind.Replace))
            {
                FileTransactionPlan.EnsureNoReparsePoint(entry.Path,
                    FileTransactionPlan.FindContainingRoot(plan.AllowedRootsUnsafe, entry.Path));
                if (!File.Exists(entry.Path) ||
                    !HashFile(entry.Path).Equals(entry.OriginalSha256, StringComparison.OrdinalIgnoreCase))
                    throw new IOException($"'{entry.Path}' changed while the transaction was being committed");
                WriteAtomic(entry.Path, Convert.FromBase64String(entry.ReplacementBytesBase64!));
            }

            foreach (var entry in entries.Where(entry => entry.Kind == TransactionEntryKind.Delete))
            {
                if (entry.BackupPath is null)
                    throw new InvalidDataException("A deletion entry has no backup path.");
                FileTransactionPlan.EnsureNoReparsePoint(entry.Path,
                    FileTransactionPlan.FindContainingRoot(plan.AllowedRootsUnsafe, entry.Path));
                if (!File.Exists(entry.Path) ||
                    !HashFile(entry.Path).Equals(entry.OriginalSha256, StringComparison.OrdinalIgnoreCase))
                    throw new IOException($"'{entry.Path}' changed while the transaction was being committed");
                File.Move(entry.Path, entry.BackupPath, overwrite: false);
            }

            // Removing the manifest is the commit point. Until then recovery restores every original.
            File.Delete(manifestPath);
        }
        catch (Exception failure)
        {
            if (!manifestWritten)
                throw;

            try
            {
                RestoreManifest(manifestPath, manifest, plan.AllowedRootsUnsafe);
            }
            catch (Exception rollbackFailure)
            {
                throw new IOException(
                    $"The file transaction failed ({failure.Message}) and rollback is incomplete ({rollbackFailure.Message}). " +
                    $"Recovery evidence remains at '{manifestPath}'.",
                    new AggregateException(failure, rollbackFailure));
            }
            throw new IOException(
                $"The file transaction failed and all original generations were restored: {failure.Message}",
                failure);
        }

        var cleanupWarnings = new List<string>();
        foreach (var entry in entries.Where(entry => entry.Kind == TransactionEntryKind.Delete))
        {
            try
            {
                if (entry.BackupPath is not null && File.Exists(entry.BackupPath))
                    File.Delete(entry.BackupPath);
            }
            catch (Exception exception)
            {
                cleanupWarnings.Add($"{entry.BackupPath}: {exception.Message}");
            }
        }

        return new FileTransactionResult(
            entries.Where(entry => entry.Kind == TransactionEntryKind.Delete).Select(entry => entry.Path).ToArray(),
            cleanupWarnings);
    }

    /// <summary>
    /// Restores interrupted transactions beneath a journal root. The caller supplies the exact
    /// allowed-root set and holds all associated leases; untrusted or newer generations stop recovery.
    /// </summary>
    public static IReadOnlyList<string> RecoverInterrupted(string transactionRoot, IEnumerable<string> allowedRoots)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transactionRoot);
        ArgumentNullException.ThrowIfNull(allowedRoots);
        var root = FileTransactionPlan.CanonicalDirectory(transactionRoot);
        var roots = allowedRoots.Select(FileTransactionPlan.CanonicalDirectory)
            .Distinct(FileTransactionPlan.PathComparer)
            .Order(FileTransactionPlan.PathComparer)
            .ToArray();
        if (roots.Length is < 1 or > 32 || !Directory.Exists(root) ||
            !FileTransactionPlan.IsUnderAnyRoot(roots, root, allowRoot: true))
        {
            throw new ArgumentException("Recovery requires an existing journal directory inside one of the allowed roots.", nameof(transactionRoot));
        }

            FileTransactionPlan.EnsureNoReparsePoint(
            root,
            FileTransactionPlan.FindContainingRoot(roots, root, allowRoot: true));
        var restored = new List<string>();
        foreach (var manifestPath in Directory.EnumerateFiles(root, ".*" + ManifestSuffix, SearchOption.TopDirectoryOnly)
                     .Order(FileTransactionPlan.PathComparer))
        {
            try
            {
                FileTransactionPlan.EnsureNoReparsePoint(manifestPath, root);
                TransactionManifest manifest;
                using (var stream = new FileStream(manifestPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (stream.Length > MaximumManifestBytes)
                        throw new InvalidDataException("transaction manifest exceeds the size limit");
                    manifest = JsonSerializer.Deserialize<TransactionManifest>(stream, ManifestOptions)
                        ?? throw new InvalidDataException("transaction manifest is empty");
                }
                ValidateManifest(root, roots, manifestPath, manifest);
                restored.AddRange(RestoreManifest(manifestPath, manifest, roots));
            }
            catch (Exception exception)
            {
                throw new FileTransactionRecoveryException(manifestPath, exception);
            }
        }

        return restored.Distinct(FileTransactionPlan.PathComparer).ToArray();
    }

    private static List<TransactionEntry> CreateManifestEntries(FileTransactionPlan plan, string transactionId)
    {
        var result = new List<TransactionEntry>();
        foreach (var entry in plan.Entries.Where(entry => entry.Kind == FileTransactionEntryKind.Replace))
        {
            result.Add(new TransactionEntry
            {
                Kind = TransactionEntryKind.Replace,
                Path = entry.Path,
                OriginalSha256 = entry.OriginalSha256!,
                OriginalBytesBase64 = Convert.ToBase64String(entry.OriginalBytes!),
                ReplacementSha256 = FileTransactionPlan.Hash(entry.ReplacementBytes!),
                ReplacementBytesBase64 = Convert.ToBase64String(entry.ReplacementBytes!)
            });
        }

        foreach (var entry in plan.Entries.Where(entry => entry.Kind == FileTransactionEntryKind.Delete && entry.Existed))
        {
            result.Add(new TransactionEntry
            {
                Kind = TransactionEntryKind.Delete,
                Path = entry.Path,
                BackupPath = entry.Path + "." + transactionId + BackupSuffix,
                OriginalSha256 = entry.OriginalSha256!
            });
        }

        return result;
    }

    private static void ValidateManifest(
        string transactionRoot,
        IReadOnlyList<string> allowedRoots,
        string manifestPath,
        TransactionManifest manifest)
    {
        if (manifest.Version != ManifestVersion || !Guid.TryParseExact(manifest.TransactionId, "N", out _))
            throw new InvalidDataException("transaction version or id is invalid");
        if (!PathsEqual(transactionRoot, manifest.TransactionRoot) ||
            !PathsEqual(ManifestPath(transactionRoot, manifest.TransactionId), manifestPath))
        {
            throw new InvalidDataException("transaction root or manifest filename does not match its transaction id");
        }
        if (!IsCanonicalDirectory(manifest.TransactionRoot))
            throw new InvalidDataException("transaction root is not canonical");

        var manifestRoots = manifest.AllowedRoots.Select(FileTransactionPlan.CanonicalDirectory)
            .Distinct(FileTransactionPlan.PathComparer)
            .Order(FileTransactionPlan.PathComparer)
            .ToArray();
        if (!manifestRoots.SequenceEqual(allowedRoots, FileTransactionPlan.PathComparer))
            throw new InvalidDataException("the manifest allowed-root set differs from the caller's allowed-root set");
        if (manifest.AllowedRoots.Any(root => !IsCanonicalDirectory(root)))
            throw new InvalidDataException("an allowed root is not canonical");
        if (manifest.Entries is null || manifest.Entries.Count is < 1 or > 16_384)
            throw new InvalidDataException("transaction entry count is invalid");

        var paths = new HashSet<string>(FileTransactionPlan.PathComparer);
        foreach (var entry in manifest.Entries)
        {
            if (entry is null || !Enum.IsDefined(entry.Kind))
                throw new InvalidDataException("transaction entry kind is invalid");
            var path = CanonicalManifestPath(entry.Path, "target");
            if (!FileTransactionPlan.IsUnderAnyRoot(allowedRoots, path, allowRoot: false) || !paths.Add(path))
                throw new InvalidDataException($"target path is outside the allowed roots or duplicated: {path}");
            var containingRoot = FileTransactionPlan.FindContainingRoot(allowedRoots, path);
            FileTransactionPlan.EnsureNoReparsePoint(path, containingRoot);
            ValidateHash(entry.OriginalSha256, "original");

            if (entry.Kind == TransactionEntryKind.Delete)
            {
                var expectedBackup = path + "." + manifest.TransactionId + BackupSuffix;
                if (entry.BackupPath is null || !PathsEqual(expectedBackup, CanonicalManifestPath(entry.BackupPath, "backup")) ||
                    entry.OriginalBytesBase64 is not null || entry.ReplacementSha256 is not null || entry.ReplacementBytesBase64 is not null)
                {
                    throw new InvalidDataException("deletion backup or fields do not match the transaction");
                }
                FileTransactionPlan.EnsureNoReparsePoint(entry.BackupPath, containingRoot);
            }
            else
            {
                if (entry.BackupPath is not null || entry.OriginalBytesBase64 is null ||
                    entry.ReplacementBytesBase64 is null || entry.ReplacementSha256 is null)
                {
                    throw new InvalidDataException("replacement recovery data is incomplete");
                }
                ValidateHash(entry.ReplacementSha256, "replacement");
                byte[] original;
                byte[] replacement;
                try
                {
                    original = Convert.FromBase64String(entry.OriginalBytesBase64);
                    replacement = Convert.FromBase64String(entry.ReplacementBytesBase64);
                }
                catch (FormatException exception)
                {
                    throw new InvalidDataException("replacement recovery bytes are invalid", exception);
                }
                if (!FileTransactionPlan.Hash(original).Equals(entry.OriginalSha256, StringComparison.OrdinalIgnoreCase) ||
                    !FileTransactionPlan.Hash(replacement).Equals(entry.ReplacementSha256, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException("replacement recovery bytes do not match their hashes");
                }
            }
        }
    }

    private static IReadOnlyList<string> RestoreManifest(
        string manifestPath,
        TransactionManifest manifest,
        IReadOnlyList<string> allowedRoots)
    {
        var deleteActions = new List<TransactionEntry>();
        var replacementActions = new List<TransactionEntry>();
        foreach (var entry in manifest.Entries)
        {
            if (entry.Kind == TransactionEntryKind.Delete)
            {
                var sourceExists = File.Exists(entry.Path) || Directory.Exists(entry.Path);
                var backupExists = File.Exists(entry.BackupPath!) || Directory.Exists(entry.BackupPath!);
                if (sourceExists && backupExists)
                    throw new IOException($"both the target and transaction backup exist for '{entry.Path}'");
                if (!sourceExists && !backupExists)
                    throw new IOException($"both the target and transaction backup are missing for '{entry.Path}'");

                var survivingPath = backupExists ? entry.BackupPath! : entry.Path;
                var containingRoot = FileTransactionPlan.FindContainingRoot(allowedRoots, entry.Path);
                FileTransactionPlan.EnsureNoReparsePoint(entry.Path, containingRoot);
                FileTransactionPlan.EnsureNoReparsePoint(survivingPath, containingRoot);
                if (Directory.Exists(survivingPath) || !File.Exists(survivingPath) ||
                    !HashFile(survivingPath).Equals(entry.OriginalSha256, StringComparison.OrdinalIgnoreCase))
                {
                    throw new IOException($"'{survivingPath}' changed after the transaction began");
                }
                if (backupExists)
                    deleteActions.Add(entry);
            }
            else
            {
                if (!File.Exists(entry.Path) || Directory.Exists(entry.Path))
                    throw new IOException($"replacement target is missing: '{entry.Path}'");
                FileTransactionPlan.EnsureNoReparsePoint(
                    entry.Path,
                    FileTransactionPlan.FindContainingRoot(allowedRoots, entry.Path));
                var currentHash = HashFile(entry.Path);
                if (currentHash.Equals(entry.OriginalSha256, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!currentHash.Equals(entry.ReplacementSha256, StringComparison.OrdinalIgnoreCase))
                    throw new IOException($"'{entry.Path}' changed after the transaction began");
                replacementActions.Add(entry);
            }
        }

        var restored = new List<string>();
        foreach (var entry in deleteActions.AsEnumerable().Reverse())
        {
            var root = FileTransactionPlan.FindContainingRoot(allowedRoots, entry.Path);
            FileTransactionPlan.EnsureNoReparsePoint(entry.Path, root);
            FileTransactionPlan.EnsureNoReparsePoint(entry.BackupPath!, root);
            if (File.Exists(entry.Path) || Directory.Exists(entry.Path) ||
                !File.Exists(entry.BackupPath!) || !HashFile(entry.BackupPath!).Equals(entry.OriginalSha256, StringComparison.OrdinalIgnoreCase))
                throw new IOException($"'{entry.Path}' changed while recovery was in progress");
            File.Move(entry.BackupPath!, entry.Path, overwrite: false);
            restored.Add(entry.Path);
        }

        foreach (var entry in replacementActions.AsEnumerable().Reverse())
        {
            var root = FileTransactionPlan.FindContainingRoot(allowedRoots, entry.Path);
            FileTransactionPlan.EnsureNoReparsePoint(entry.Path, root);
            if (!File.Exists(entry.Path) ||
                !HashFile(entry.Path).Equals(entry.ReplacementSha256, StringComparison.OrdinalIgnoreCase))
                throw new IOException($"'{entry.Path}' changed while recovery was in progress");
            var original = Convert.FromBase64String(entry.OriginalBytesBase64!);
            WriteAtomic(entry.Path, original);
            restored.Add(entry.Path);
        }

        File.Delete(manifestPath);
        return restored;
    }

    private static void WriteManifest(string manifestPath, TransactionManifest manifest)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(manifest, ManifestOptions);
        if (bytes.Length > MaximumManifestBytes)
            throw new InvalidDataException("transaction manifest exceeds the size limit");
        var temporaryPath = manifestPath + ".tmp";
        var temporaryCreated = false;
        try
        {
            WriteDurableBytes(temporaryPath, bytes);
            temporaryCreated = true;
            File.Move(temporaryPath, manifestPath, overwrite: false);
            temporaryCreated = false;
        }
        finally
        {
            if (temporaryCreated && File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    private static void WriteAtomic(string path, byte[] bytes)
    {
        var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        var temporaryCreated = false;
        try
        {
            WriteDurableBytes(temporaryPath, bytes);
            temporaryCreated = true;
            File.Move(temporaryPath, path, overwrite: true);
            temporaryCreated = false;
        }
        finally
        {
            if (temporaryCreated && File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    private static void WriteDurableBytes(string path, byte[] bytes)
    {
        FileStream? stream = null;
        try
        {
            stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough);
            stream.Write(bytes);
            stream.Flush(flushToDisk: true);
            stream.Dispose();
        }
        catch
        {
            stream?.Dispose();
            if (stream is not null && File.Exists(path))
                File.Delete(path);
            throw;
        }
    }

    private static string ManifestPath(string transactionRoot, string transactionId) =>
        Path.Combine(transactionRoot, "." + transactionId + ManifestSuffix);

    private static string CanonicalManifestPath(string path, string description)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new InvalidDataException($"{description} path is missing");
        var canonical = Path.GetFullPath(path);
        if (!PathsEqual(canonical, path))
            throw new InvalidDataException($"{description} path is not canonical: {path}");
        return canonical;
    }

    private static bool PathsEqual(string left, string right) =>
        string.Equals(Path.GetFullPath(left), Path.GetFullPath(right),
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

    private static bool IsCanonicalDirectory(string path) =>
        string.Equals(FileTransactionPlan.CanonicalDirectory(path), path,
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

    private static string HashFile(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private static void ValidateHash(string? value, string description)
    {
        if (value is null || value.Length != 64 || value.Any(character => !Uri.IsHexDigit(character)))
            throw new InvalidDataException($"{description} SHA-256 is invalid");
    }

    private enum TransactionEntryKind
    {
        Delete,
        Replace
    }

    private sealed class TransactionManifest
    {
        public TransactionManifest() { }

        public int Version { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string TransactionRoot { get; set; } = string.Empty;
        public List<string> AllowedRoots { get; set; } = new();
        public List<TransactionEntry> Entries { get; set; } = new();
    }

    private sealed class TransactionEntry
    {
        public TransactionEntry() { }

        public TransactionEntryKind Kind { get; set; }
        public string Path { get; set; } = string.Empty;
        public string? BackupPath { get; set; }
        public string OriginalSha256 { get; set; } = string.Empty;
        public string? OriginalBytesBase64 { get; set; }
        public string? ReplacementSha256 { get; set; }
        public string? ReplacementBytesBase64 { get; set; }
    }
}
