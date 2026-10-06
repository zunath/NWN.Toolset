using System.Security.Cryptography;

namespace Nwn.Authoring.Editing;

/// <summary>Captures file generations and allowed roots for one removal/replacement transaction.</summary>
/// <remarks>
/// The host supplies resource paths and holds its established cross-process leases while committing
/// or recovering the plan. This type deliberately does not choose lock order or resource policy.
/// </remarks>
public sealed class FileTransactionPlan
{
    private const int MaximumPathCount = 16_384;
    private const int MaximumReplacementBytes = 64 * 1024 * 1024;
    private readonly IReadOnlyList<string> _allowedRoots;
    private readonly IReadOnlyList<FileTransactionPlanEntry> _entries;

    private FileTransactionPlan(string transactionRoot, IReadOnlyList<string> allowedRoots, IReadOnlyList<FileTransactionPlanEntry> entries)
    {
        TransactionRoot = transactionRoot;
        _allowedRoots = allowedRoots;
        _entries = entries;
    }

    /// <summary>The directory in which the durable transaction manifest is stored.</summary>
    public string TransactionRoot { get; }

    /// <summary>Returns canonical roots that constrain all transaction and recovery paths.</summary>
    public IReadOnlyList<string> AllowedRoots => _allowedRoots.ToArray();

    /// <summary>Returns the captured canonical mutation paths.</summary>
    public IReadOnlyList<string> AffectedPaths => _entries.Select(entry => entry.Path).ToArray();

    /// <summary>Returns deletion targets that existed when this plan was captured.</summary>
    public IReadOnlyList<string> ExistingDeletionPaths => _entries
        .Where(entry => entry.Kind == FileTransactionEntryKind.Delete && entry.Existed)
        .Select(entry => entry.Path)
        .ToArray();

    /// <summary>
    /// Captures deletion targets, including absent targets, and guarded replacements. A later
    /// appearance, removal, or byte change makes commit fail before it writes a manifest.
    /// </summary>
    public static FileTransactionPlan Capture(
        string transactionRoot,
        IEnumerable<string> allowedRoots,
        IEnumerable<string> deletionPaths,
        IEnumerable<FileTransactionReplacement>? replacements = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transactionRoot);
        ArgumentNullException.ThrowIfNull(allowedRoots);
        ArgumentNullException.ThrowIfNull(deletionPaths);

        var comparer = PathComparer;
        var roots = allowedRoots.Select(CanonicalDirectory).Distinct(comparer).Order(comparer).ToArray();
        if (roots.Length is < 1 or > 32)
            throw new ArgumentException("A transaction requires between one and 32 allowed roots.", nameof(allowedRoots));
        foreach (var root in roots)
        {
            if (!Directory.Exists(root))
                throw new DirectoryNotFoundException($"Allowed transaction root does not exist: {root}");
            EnsureNoReparsePoint(root, root);
        }

        var canonicalTransactionRoot = CanonicalDirectory(transactionRoot);
        if (!Directory.Exists(canonicalTransactionRoot) || !IsUnderAnyRoot(roots, canonicalTransactionRoot, allowRoot: true))
            throw new ArgumentException("The transaction root must be an existing directory inside an allowed root.", nameof(transactionRoot));
        EnsureNoReparsePoint(canonicalTransactionRoot, canonicalTransactionRoot);

        var deletePaths = deletionPaths.Select(path => CanonicalFile(path, roots)).ToArray();
        var replacementArray = (replacements ?? Array.Empty<FileTransactionReplacement>()).ToArray();
        if (deletePaths.Length + replacementArray.Length is < 1 or > MaximumPathCount)
            throw new ArgumentException($"A transaction must affect between one and {MaximumPathCount} paths.");

        var allPaths = deletePaths.Concat(replacementArray.Select(replacement =>
        {
            ArgumentNullException.ThrowIfNull(replacement);
            return CanonicalFile(replacement.Path, roots);
        })).ToArray();
        if (allPaths.Distinct(comparer).Count() != allPaths.Length)
            throw new ArgumentException("A transaction cannot contain duplicate or overlapping mutation targets.");

        var entries = new List<FileTransactionPlanEntry>(allPaths.Length);
        foreach (var path in deletePaths)
            entries.Add(CaptureDeletion(path, roots));

        for (var index = 0; index < replacementArray.Length; index++)
        {
            var replacement = replacementArray[index];
            if (replacement.ExpectedBytesUnsafe.Length > MaximumReplacementBytes ||
                replacement.ReplacementBytesUnsafe.Length > MaximumReplacementBytes)
            {
                throw new ArgumentException($"Replacement data cannot exceed {MaximumReplacementBytes} bytes.", nameof(replacements));
            }

            var path = allPaths[deletePaths.Length + index];
            EnsureNoReparsePoint(path, FindContainingRoot(roots, path));
            if (!File.Exists(path) || Directory.Exists(path))
                throw new IOException($"Replacement target does not exist as a file: {path}");
            var expected = replacement.ExpectedBytesUnsafe.ToArray();
            var actual = File.ReadAllBytes(path);
            if (!actual.AsSpan().SequenceEqual(expected))
                throw new IOException($"{Path.GetFileName(path)} does not match the expected replacement generation.");

            entries.Add(new FileTransactionPlanEntry(path, FileTransactionEntryKind.Replace, true, Hash(expected), expected,
                replacement.ReplacementBytesUnsafe.ToArray()));
        }

        if (entries.Count(entry => entry.Kind == FileTransactionEntryKind.Delete && entry.Existed) == 0 &&
            entries.All(entry => entry.Kind == FileTransactionEntryKind.Delete))
        {
            throw new FileNotFoundException("No deletion target exists in the captured transaction.");
        }

        return new FileTransactionPlan(canonicalTransactionRoot, Array.AsReadOnly(roots), entries.AsReadOnly());
    }

    internal IReadOnlyList<string> AllowedRootsUnsafe => _allowedRoots;

    internal IReadOnlyList<FileTransactionPlanEntry> Entries => _entries;

    internal void VerifyCurrentGeneration()
    {
        foreach (var entry in _entries)
        {
            EnsureNoReparsePoint(entry.Path, FindContainingRoot(_allowedRoots, entry.Path));
            var exists = File.Exists(entry.Path);
            if (Directory.Exists(entry.Path) || exists != entry.Existed)
                throw new IOException($"{Path.GetFileName(entry.Path)} changed while the transaction was being prepared.");
            if (!exists)
                continue;

            if (!HashFile(entry.Path).Equals(entry.OriginalSha256, StringComparison.OrdinalIgnoreCase))
                throw new IOException($"{Path.GetFileName(entry.Path)} changed while the transaction was being prepared.");
        }
    }

    internal static StringComparer PathComparer => OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    internal static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));

    private static string HashFile(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    internal static string CanonicalFile(string path, IReadOnlyList<string> roots)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var canonical = Path.GetFullPath(path);
        if (Path.GetFileName(canonical).Length == 0 || !IsUnderAnyRoot(roots, canonical, allowRoot: false))
            throw new ArgumentException($"Mutation path must be a file inside an allowed root: {path}");
        EnsureNoReparsePoint(canonical, FindContainingRoot(roots, canonical));
        return canonical;
    }

    internal static string CanonicalDirectory(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
    }

    internal static bool IsUnderRoot(string root, string path, bool allowRoot = false)
    {
        var canonicalRoot = CanonicalDirectory(root);
        var canonicalPath = Path.GetFullPath(path);
        if (allowRoot && canonicalPath.Equals(canonicalRoot, PathComparison))
            return true;
        var relative = Path.GetRelativePath(canonicalRoot, canonicalPath);
        return !Path.IsPathRooted(relative) && relative != "." && relative != ".." &&
               !relative.StartsWith(".." + Path.DirectorySeparatorChar, PathComparison);
    }

    internal static void EnsureNoReparsePoint(string path, string root)
    {
        var canonicalRoot = CanonicalDirectory(root);
        var canonicalPath = Path.GetFullPath(path);
        if (!IsUnderRoot(canonicalRoot, canonicalPath, allowRoot: true))
            throw new InvalidDataException($"Path escapes its allowed root: {path}");

        var current = Path.GetPathRoot(canonicalRoot)
            ?? throw new InvalidDataException($"Allowed root has no filesystem root: {canonicalRoot}");
        RefuseReparsePoint(current);
        var rootRelative = Path.GetRelativePath(current, canonicalRoot);
        if (rootRelative != ".")
        {
            foreach (var segment in rootRelative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
            {
                current = Path.Combine(current, segment);
                RefuseReparsePoint(current);
            }
        }

        var relative = Path.GetRelativePath(canonicalRoot, canonicalPath);
        if (relative == ".")
            return;

        foreach (var segment in relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            current = Path.Combine(current, segment);
            RefuseReparsePoint(current);
        }
    }

    internal static bool IsUnderAnyRoot(IReadOnlyList<string> roots, string path, bool allowRoot) =>
        roots.Any(root => IsUnderRoot(root, path, allowRoot));

    internal static string FindContainingRoot(IReadOnlyList<string> roots, string path, bool allowRoot = false) =>
        roots.FirstOrDefault(root => IsUnderRoot(root, path, allowRoot))
        ?? throw new InvalidDataException($"Path is outside the allowed roots: {path}");

    private static FileTransactionPlanEntry CaptureDeletion(string path, IReadOnlyList<string> roots)
    {
        EnsureNoReparsePoint(path, FindContainingRoot(roots, path));
        if (Directory.Exists(path))
            throw new IOException($"Deletion target is a directory, not a file: {path}");
        if (!File.Exists(path))
            return new FileTransactionPlanEntry(path, FileTransactionEntryKind.Delete, false, null, null, null);

        return new FileTransactionPlanEntry(path, FileTransactionEntryKind.Delete, true, HashFile(path), null, null);
    }

    private static void RefuseReparsePoint(string path)
    {
        try
        {
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException($"Transaction paths cannot traverse symbolic links or reparse points: {path}");
        }
        catch (FileNotFoundException)
        {
        }
        catch (DirectoryNotFoundException)
        {
        }
    }

    private static StringComparison PathComparison => OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

}
