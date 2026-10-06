namespace Nwn.Preview.Cache;

/// <summary>A thread-safe least-recently-used cache bounded by retained bytes and entry count.</summary>
public sealed class BoundedLruCache<TKey, TValue>
    where TKey : notnull
    where TValue : class
{
    private readonly object _gate = new();
    private readonly long _maximumBytes;
    private readonly int _maximumEntries;
    private readonly Func<TValue?, long> _sizeOf;
    private readonly Dictionary<TKey, LinkedListNode<Entry>> _entries;
    private readonly LinkedList<Entry> _order = new();
    private long _heldBytes;

    public BoundedLruCache(
        long maximumBytes,
        int maximumEntries,
        Func<TValue?, long> sizeOf,
        IEqualityComparer<TKey>? comparer = null)
    {
        if (maximumBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(maximumBytes));
        if (maximumEntries <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximumEntries));
        _maximumBytes = maximumBytes;
        _maximumEntries = maximumEntries;
        _sizeOf = sizeOf ?? throw new ArgumentNullException(nameof(sizeOf));
        _entries = new Dictionary<TKey, LinkedListNode<Entry>>(comparer);
    }

    public long HeldBytes
    {
        get { lock (_gate) return _heldBytes; }
    }

    public TValue? GetOrAdd(TKey key, Func<TValue?> create)
    {
        ArgumentNullException.ThrowIfNull(create);
        lock (_gate)
        {
            if (_entries.TryGetValue(key, out var cached))
            {
                Touch(cached);
                return cached.Value.Value;
            }
        }

        var value = create();
        var size = _sizeOf(value);
        if (size < 0)
            throw new InvalidOperationException("A cached value cannot have a negative size.");

        lock (_gate)
        {
            if (_entries.TryGetValue(key, out var existing))
            {
                Touch(existing);
                return existing.Value.Value;
            }

            if (size > _maximumBytes)
                return value;

            var node = _order.AddFirst(new Entry(key, value, size));
            _entries.Add(key, node);
            _heldBytes = checked(_heldBytes + size);
            while (_heldBytes > _maximumBytes || _entries.Count > _maximumEntries)
                EvictOldest();
            return value;
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _entries.Clear();
            _order.Clear();
            _heldBytes = 0;
        }
    }

    private void Touch(LinkedListNode<Entry> node)
    {
        _order.Remove(node);
        _order.AddFirst(node);
    }

    private void EvictOldest()
    {
        var oldest = _order.Last ?? throw new InvalidOperationException("The cache accounting is inconsistent.");
        _order.RemoveLast();
        _entries.Remove(oldest.Value.Key);
        _heldBytes -= oldest.Value.SizeBytes;
    }

    private sealed record Entry(TKey Key, TValue? Value, long SizeBytes);
}
