using Nwn.Formats.Erf;
using Nwn.Formats.Key;
using Nwn.Formats.Resources;

namespace Nwn.Authoring.Resources;

/// <summary>An explicitly configured source of resources. No game, user, or override directories
/// are discovered implicitly.</summary>
public sealed class ResourceLayer : IDisposable
{
    private readonly Dictionary<ResourceIdentity, List<ResourceItem>> _items = [];
    private readonly List<IDisposable> _owned = [];
    private readonly ResourceDuplicatePolicy _duplicatePolicy;

    public string Name { get; }
    public ResourceLayerKind Kind { get; }
    public IReadOnlyCollection<ResourceIdentity> Resources => _items.Keys.ToArray();
    public DateTime ContentVersionUtc { get; private set; } = DateTime.MinValue;

    private ResourceLayer(string name, ResourceLayerKind kind, ResourceDuplicatePolicy duplicatePolicy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("A resource layer needs a name.", nameof(name));
        if (!Enum.IsDefined(duplicatePolicy))
            throw new ArgumentOutOfRangeException(nameof(duplicatePolicy));
        Name = name;
        Kind = kind;
        _duplicatePolicy = duplicatePolicy;
    }

    /// <summary>Indexes only files in this directory. The caller explicitly selects each directory
    /// and its position in the resolver's ordered layer list.</summary>
    public static ResourceLayer FromLooseDirectory(
        string name,
        string directory,
        ResourceDuplicatePolicy duplicatePolicy = ResourceDuplicatePolicy.Reject,
        ResourceLayerReadOptions? readOptions = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        var fullDirectory = Path.GetFullPath(directory);
        if (!Directory.Exists(fullDirectory))
            throw new DirectoryNotFoundException(fullDirectory);
        var layer = new ResourceLayer(name, ResourceLayerKind.LooseDirectory, duplicatePolicy);
        readOptions ??= new ResourceLayerReadOptions();
        readOptions.Validate();
        foreach (var path in Directory.EnumerateFiles(fullDirectory)
                     .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(Path.GetFileName, StringComparer.Ordinal))
        {
            var fileName = Path.GetFileName(path);
            if (fileName.StartsWith(".", StringComparison.Ordinal))
                continue;
            var extension = Path.GetExtension(path).TrimStart('.');
            if (!ResourceTypes.TryGetByExtension(extension, out var type))
                continue;
            var stem = Path.GetFileNameWithoutExtension(path);
            var identity = new ResourceIdentity(stem, type);
            layer.Add(identity, path, null, fileName, maximumBytes =>
                ReadFileBounded(path, Math.Min(readOptions.MaximumResourceBytes, maximumBytes), "resource"));
            layer.ContentVersionUtc = Max(layer.ContentVersionUtc, File.GetLastWriteTimeUtc(path));
        }
        layer.ValidateDuplicates();
        return layer;
    }

    /// <summary>Indexes an explicitly selected ERF, HAK, or MOD. Resource payload streams are
    /// opened only for bounded reads, so an indexed handle remains independent of a resolver
    /// generation's lifetime.</summary>
    public static ResourceLayer FromErf(
        string name,
        string archivePath,
        ResourceDuplicatePolicy duplicatePolicy = ResourceDuplicatePolicy.Reject,
        ResourceLayerReadOptions? readOptions = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(archivePath);
        readOptions ??= new ResourceLayerReadOptions();
        readOptions.Validate();
        archivePath = Path.GetFullPath(archivePath);
        using var archive = ErfArchive.Open(archivePath);
        var layer = new ResourceLayer(name, ResourceLayerKind.ErfArchive, duplicatePolicy);
        layer.ContentVersionUtc = File.GetLastWriteTimeUtc(Path.GetFullPath(archivePath));
        foreach (var entry in archive.Entries)
        {
            layer.Add(new ResourceIdentity(entry.ResRef.Value, entry.Type), archivePath, archivePath, entry.FileName,
                maximumBytes =>
                {
                    var limit = Math.Min(readOptions.MaximumResourceBytes, maximumBytes);
                    if (entry.Size > limit || entry.Size > Array.MaxLength)
                        throw new FormatException($"ERF resource '{entry.ResRef}' is {entry.Size} bytes; configured limit is {limit}.");
                    return ReadErfEntry(archivePath, entry);
                });
        }
        layer.ValidateDuplicates();
        return layer;
    }

    /// <summary>Indexes one explicitly selected KEY and the BIF files resolved beneath its supplied
    /// data root. No installation paths or user override locations are guessed.</summary>
    public static ResourceLayer FromKeyBif(
        string name,
        string keyPath,
        string dataRoot,
        ResourceDuplicatePolicy duplicatePolicy = ResourceDuplicatePolicy.Reject,
        ResourceLayerReadOptions? readOptions = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(dataRoot);
        var root = Path.GetFullPath(dataRoot);
        readOptions ??= new ResourceLayerReadOptions();
        readOptions.Validate();
        var key = KeyReader.Read(ReadFileBounded(keyPath, readOptions.MaximumKeyFileBytes, "KEY index"));
        var stock = StockArchive.FromStreams(key, filename =>
        {
            var path = ResolveBifPath(root, filename);
            var stream = File.OpenRead(path);
            if (stream.Length <= readOptions.MaximumBifFileBytes) return stream;
            var length = stream.Length;
            stream.Dispose();
            throw new FormatException($"BIF archive '{path}' is {length} bytes; configured limit is {readOptions.MaximumBifFileBytes}.");
        }, readOptions.MaximumCachedBifBytes);
        var layer = new ResourceLayer(name, ResourceLayerKind.KeyBif, duplicatePolicy);
        layer.ContentVersionUtc = key.Resources.Select(entry => ResolveBifPath(root, key.Bifs[entry.BifIndex].Filename))
            .Where(File.Exists).Select(File.GetLastWriteTimeUtc).Append(File.GetLastWriteTimeUtc(Path.GetFullPath(keyPath))).Max();
        foreach (var entry in key.Resources)
        {
            if (entry.Type is not { } type)
                continue;
            var bifName = key.Bifs[entry.BifIndex].Filename;
            var bifPath = ResolveBifPath(root, bifName);
            layer.Add(new ResourceIdentity(entry.ResRef, type), bifPath, keyPath, bifName,
                maximumBytes => stock.ReadResource(entry, checked((int)Math.Min(
                    Math.Min(readOptions.MaximumResourceBytes, maximumBytes), int.MaxValue))));
        }
        layer.ValidateDuplicates();
        return layer;
    }

    internal IReadOnlyList<ResourceItem> Find(ResourceIdentity identity) =>
        _items.TryGetValue(identity, out var items) ? items : [];

    internal ResourceItem Select(IReadOnlyList<ResourceItem> items) => _duplicatePolicy switch
    {
        ResourceDuplicatePolicy.FirstWins => items[0],
        ResourceDuplicatePolicy.LastWins => items[^1],
        _ => items[0]
    };

    internal IEnumerable<ResourceItem> AllExcept(ResourceIdentity identity, ResourceItem selected) =>
        _items[identity].Where(item => !ReferenceEquals(item, selected));

    private void Add(ResourceIdentity identity, string sourcePath, string? containerPath, string entry, Func<long, byte[]> read)
    {
        if (!_items.TryGetValue(identity, out var items))
            _items[identity] = items = [];
        items.Add(new ResourceItem(new ResourceProvenance(Name, Kind, Path.GetFullPath(sourcePath),
            containerPath is null ? null : Path.GetFullPath(containerPath), entry), read));
    }

    private void ValidateDuplicates()
    {
        if (_duplicatePolicy != ResourceDuplicatePolicy.Reject)
            return;
        var duplicate = _items.FirstOrDefault(pair => pair.Value.Count > 1);
        if (duplicate.Value is not null)
            throw new FormatException($"Layer '{Name}' contains duplicate resource '{duplicate.Key.Resref}.{ResourceTypes.GetExtension(duplicate.Key.Type)}'.");
    }

    private static string ResolveBifPath(string root, string filename)
    {
        var relative = filename.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        var path = Path.GetFullPath(Path.Combine(root, relative));
        var prefix = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new FormatException($"KEY BIF path '{filename}' escapes the configured data root.");
        return path;
    }

    private static DateTime Max(DateTime left, DateTime right) => left >= right ? left : right;

    // Reads one indexed entry by offset. Re-parsing the archive's key and resource lists on every
    // read cost each texture, model and 2DA lookup a full header scan of a multi-thousand-entry HAK.
    private static byte[] ReadErfEntry(string archivePath, ErfEntry entry)
    {
        using var handle = File.OpenHandle(archivePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var length = RandomAccess.GetLength(handle);
        if (entry.Offset > length || entry.Size > length - entry.Offset)
            throw new FormatException($"ERF resource '{entry.ResRef}' no longer fits within '{archivePath}' (length {length}).");
        var bytes = new byte[checked((int)entry.Size)];
        var read = 0;
        while (read < bytes.Length)
        {
            var count = RandomAccess.Read(handle, bytes.AsSpan(read), entry.Offset + read);
            if (count == 0)
                throw new EndOfStreamException($"ERF resource '{entry.ResRef}' ended early in '{archivePath}'.");
            read += count;
        }
        return bytes;
    }

    private static byte[] ReadFileBounded(string path, long maximumBytes, string description)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length > maximumBytes || stream.Length > Array.MaxLength)
            throw new FormatException($"{description} '{path}' is {stream.Length} bytes; configured limit is {maximumBytes}.");
        var bytes = new byte[(int)stream.Length];
        stream.ReadExactly(bytes);
        if (stream.ReadByte() != -1)
            throw new IOException($"{description} '{path}' grew while it was being read.");
        return bytes;
    }

    public void Dispose()
    {
        foreach (var item in _owned)
            item.Dispose();
        _owned.Clear();
    }

    internal sealed record ResourceItem(ResourceProvenance Provenance, Func<long, byte[]> Read);
}
