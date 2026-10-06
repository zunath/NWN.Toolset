using Nwn.Formats.Resources;

namespace Nwn.Formats.Key;

/// <summary>
/// Ties a parsed KEY to explicitly supplied BIF sources. Stream sources retain only bounded
/// metadata and open one requested payload; byte-array sources retain their supplied snapshots.
/// </summary>
public sealed class StockArchive
{
    private readonly KeyFile _key;
    private readonly Func<string, byte[]>? _loadBif;
    private readonly Func<string, Stream>? _openBif;
    private readonly long _maximumCachedIndexBytes;
    private readonly Dictionary<string, (byte[] Bytes, BifFile Parsed)> _bifCache = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (long Length, BifFile Parsed)> _bifIndexCache = new(StringComparer.Ordinal);
    private readonly object _sync = new();
    private long _cachedBifBytes;

    /// <summary>Retained snapshot bytes, or the conservative metadata budget for stream sources.</summary>
    public long CachedBifBytes
    {
        get
        {
            lock (_sync)
            {
                return _cachedBifBytes;
            }
        }
    }

    /// <param name="key">The parsed KEY index.</param>
    /// <param name="loadBif">Given a <see cref="KeyBifEntry.Filename"/>, returns that BIF's bytes.</param>
    public StockArchive(KeyFile key, Func<string, byte[]> loadBif)
    {
        _key = key;
        _loadBif = loadBif;
    }

    private StockArchive(KeyFile key, Func<string, Stream> openBif, long maximumCachedIndexBytes)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(openBif);
        ArgumentOutOfRangeException.ThrowIfNegative(maximumCachedIndexBytes);
        _key = key;
        _openBif = openBif;
        _maximumCachedIndexBytes = maximumCachedIndexBytes;
    }

    public static StockArchive FromStreams(KeyFile key, Func<string, Stream> openBif,
        long maximumCachedIndexBytes = 64L * 1024 * 1024) => new(key, openBif, maximumCachedIndexBytes);

    public byte[] ReadResource(Resref resRef, ResourceType type) => ReadResource(FindOrThrow(resRef, type));

    public byte[] ReadResource(KeyResourceEntry entry)
        => ReadResource(entry, long.MaxValue);

    /// <summary>Reads one resource only when its declared size is within the caller's allocation limit.</summary>
    public byte[] ReadResource(KeyResourceEntry entry, long maximumResourceBytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maximumResourceBytes);
        lock (_sync)
        {
            var bifEntry = _key.Bifs[entry.BifIndex];
            if (_openBif is not null)
                return ReadStreamingResource(entry, bifEntry, maximumResourceBytes);
            if (!_bifCache.TryGetValue(bifEntry.Filename, out var cached))
            {
                var bytes = _loadBif!(bifEntry.Filename);
                var parsed = BifReader.Read(bytes);
                var retainedByteCount = checked(_cachedBifBytes + bytes.LongLength);
                cached = (bytes, parsed);
                _bifCache.Add(bifEntry.Filename, cached);
                _cachedBifBytes = retainedByteCount;
            }
            var resourceEntry = cached.Parsed.Find(entry.IndexInBif)
                ?? throw new FormatException($"BIF '{bifEntry.Filename}' has no resource at index {entry.IndexInBif} (needed for '{entry.ResRef}').");
            if (resourceEntry.FileSize > maximumResourceBytes || resourceEntry.FileSize > Array.MaxLength)
                throw new FormatException($"KEY/BIF resource '{entry.ResRef}' is {resourceEntry.FileSize} bytes; configured limit is {maximumResourceBytes}.");
            return BifReader.ReadResource(cached.Bytes, resourceEntry);
        }
    }

    private byte[] ReadStreamingResource(KeyResourceEntry entry, KeyBifEntry bifEntry, long maximumResourceBytes)
    {
        using var stream = _openBif!(bifEntry.Filename);
        if (!_bifIndexCache.TryGetValue(bifEntry.Filename, out var cached))
        {
            var parsed = BifReader.Read(stream, _maximumCachedIndexBytes - _cachedBifBytes);
            cached = (stream.Length, parsed);
            _bifIndexCache.Add(bifEntry.Filename, cached);
            _cachedBifBytes = checked(_cachedBifBytes + parsed.IndexBudgetBytes);
        }
        if (stream.Length != cached.Length)
            throw new FormatException($"BIF '{bifEntry.Filename}' changed length after its resource index was loaded.");
        var resource = cached.Parsed.Find(entry.IndexInBif)
            ?? throw new FormatException($"BIF '{bifEntry.Filename}' has no resource at index {entry.IndexInBif} (needed for '{entry.ResRef}').");
        return BifReader.ReadResource(stream, resource, maximumResourceBytes);
    }

    /// <summary>The distinct BIF filenames needed to read every one of <paramref name="resources"/> --
    /// lets a caller (e.g. an extraction script) learn exactly which BIF(s) to fetch before loading
    /// any of them.</summary>
    public IReadOnlyList<string> BifsNeededFor(IEnumerable<(Resref ResRef, ResourceType Type)> resources)
    {
        var names = new List<string>();
        foreach (var (resRef, type) in resources)
        {
            var name = _key.Bifs[FindOrThrow(resRef, type).BifIndex].Filename;
            if (!names.Contains(name, StringComparer.Ordinal))
            {
                names.Add(name);
            }
        }
        return names;
    }

    private KeyResourceEntry FindOrThrow(Resref resRef, ResourceType type) =>
        _key.Find(resRef, type)
            ?? throw new KeyNotFoundException($"Stock resource '{resRef.Value}.{ResourceTypes.GetExtension(type)}' is not in the KEY's resource index.");
}
