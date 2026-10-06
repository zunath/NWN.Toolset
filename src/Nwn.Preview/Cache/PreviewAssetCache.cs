using Nwn.Preview.Pixels;
using Nwn.Preview.Scene;

namespace Nwn.Preview.Cache;

/// <summary>Thread-safe LRU cache for prepared geometry and decoded images with explicit entry and byte budgets.</summary>
public sealed class PreviewAssetCache
{
    private readonly object _sync = new();
    private readonly int _maximumEntries;
    private readonly long _maximumBytes;
    private readonly Dictionary<CacheKey, CacheEntry> _entries = new();
    private readonly LinkedList<CacheKey> _leastToMostRecent = new();
    private long _cachedBytes;

    public PreviewAssetCache(int maximumEntries = 256, long maximumBytes = 512L * 1024 * 1024)
    {
        if (maximumEntries <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximumEntries));
        if (maximumBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximumBytes));
        _maximumEntries = maximumEntries;
        _maximumBytes = maximumBytes;
    }

    public int CachedEntryCount { get { lock (_sync) return _entries.Count; } }
    public long CachedBytes { get { lock (_sync) return _cachedBytes; } }

    public PreparedScene GetOrAddScene(string key, Func<PreparedScene> create)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(create);
        var cacheKey = new CacheKey(AssetKind.Scene, key);
        lock (_sync)
        {
            if (_entries.TryGetValue(cacheKey, out var found))
            {
                Touch(found);
                return (PreparedScene)found.Value;
            }
            var scene = create() ?? throw new InvalidOperationException("Preview scene factory returned null.");
            Add(cacheKey, scene, EstimateBytes(scene));
            return scene;
        }
    }

    public RgbaImage GetOrAddImage(string key, Func<RgbaImage> create)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(create);
        var cacheKey = new CacheKey(AssetKind.Image, key);
        lock (_sync)
        {
            if (_entries.TryGetValue(cacheKey, out var found))
            {
                Touch(found);
                return (RgbaImage)found.Value;
            }
            var image = create() ?? throw new InvalidOperationException("Preview image factory returned null.");
            Add(cacheKey, image, checked((long)image.Width * image.Height * 4));
            return image;
        }
    }

    private void Add(CacheKey key, object value, long bytes)
    {
        if (bytes > _maximumBytes)
            return;
        while (_entries.Count >= _maximumEntries || _cachedBytes > _maximumBytes - bytes)
            RemoveLeastRecent();
        var node = _leastToMostRecent.AddLast(key);
        _entries.Add(key, new CacheEntry(value, bytes, node));
        _cachedBytes += bytes;
    }

    private void Touch(CacheEntry entry)
    {
        _leastToMostRecent.Remove(entry.Node);
        _leastToMostRecent.AddLast(entry.Node);
    }

    private void RemoveLeastRecent()
    {
        var key = _leastToMostRecent.First!.Value;
        var entry = _entries[key];
        _leastToMostRecent.RemoveFirst();
        _entries.Remove(key);
        _cachedBytes -= entry.ByteCount;
    }

    private static long EstimateBytes(PreparedScene scene)
    {
        long bytes = 0;
        foreach (var node in scene.Nodes)
        {
            if (node.Mesh is not { } mesh)
                continue;
            bytes = checked(bytes + (long)mesh.Vertices.Count * 12 + (long)mesh.Normals.Count * 12 +
                            (long)mesh.TextureVertices.Count * 8 + (long)mesh.Faces.Count * 32);
        }
        return bytes;
    }

    private enum AssetKind { Scene, Image }
    private readonly record struct CacheKey(AssetKind Kind, string Value);
    private sealed record CacheEntry(object Value, long ByteCount, LinkedListNode<CacheKey> Node);
}
