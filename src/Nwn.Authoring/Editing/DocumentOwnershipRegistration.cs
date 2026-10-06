namespace Nwn.Authoring.Editing;

/// <summary>Owns one session's registrations on the mutable nodes in a parsed GFF document.</summary>
internal sealed class DocumentOwnershipRegistration : IDisposable
{
    private readonly List<WeakReference<object>> _nodes = [];
    private bool _disposed;

    internal bool IsDisposed => _disposed;

    internal void Track(object node)
    {
        if (_disposed)
            throw new InvalidOperationException("A disposed document registration cannot track nodes.");

        _nodes.Add(new WeakReference<object>(node));
    }

    internal void PruneDeadNodes()
    {
        if (_disposed)
            return;

        var write = 0;
        for (var index = 0; index < _nodes.Count; index++)
        {
            var reference = _nodes[index];
            if (!reference.TryGetTarget(out _))
                continue;

            _nodes[write++] = reference;
        }

        if (write < _nodes.Count)
            _nodes.RemoveRange(write, _nodes.Count - write);
    }

    internal IEnumerable<object> GetLiveNodes()
    {
        foreach (var reference in _nodes)
        {
            if (reference.TryGetTarget(out var node))
                yield return node;
        }
    }

    public void Dispose()
    {
        DocumentOwnershipRegistry.Release(this);
    }

    internal void MarkDisposed() => _disposed = true;

    internal void ClearNodes() => _nodes.Clear();
}
