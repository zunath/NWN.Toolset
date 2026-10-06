namespace Nwn.Authoring.Resources;

/// <summary>Resolves resources from caller-ordered layers. Earlier layers have higher precedence.</summary>
public sealed class ResourceResolver : IDisposable
{
    private readonly IReadOnlyList<ResourceLayer> _layers;
    private readonly IReadOnlyList<ResourceIdentity> _resources;
    private readonly bool _ownsLayers;

    /// <param name="layers">Explicitly ordered from highest to lowest precedence.</param>
    /// <param name="ownsLayers">Whether disposing the resolver also disposes the supplied layers.</param>
    public ResourceResolver(IEnumerable<ResourceLayer> layers, bool ownsLayers = false)
    {
        ArgumentNullException.ThrowIfNull(layers);
        _layers = layers.ToArray();
        if (_layers.Any(layer => layer is null))
            throw new ArgumentException("Resource layers cannot contain null.", nameof(layers));
        _resources = Array.AsReadOnly(_layers.SelectMany(layer => layer.Resources).Distinct().ToArray());
        _ownsLayers = ownsLayers;
    }

    /// <summary>Distinct identities indexed by the configured layers, ordered by first occurrence
    /// from highest to lowest precedence. Enumerating the inventory does not read resource bytes.</summary>
    public IReadOnlyList<ResourceIdentity> Resources => _resources;

    public ResourceResolutionHandle? ResolveHandle(ResourceIdentity identity)
    {
        var candidates = _layers.Select(layer => (Layer: layer, Items: layer.Find(identity)))
            .Where(candidate => candidate.Items.Count > 0).ToArray();
        if (candidates.Length == 0)
            return null;

        var winningLayer = candidates[0].Layer;
        var winningItem = winningLayer.Select(candidates[0].Items);
        var overridden = new List<ResourceProvenance>();
        overridden.AddRange(winningLayer.AllExcept(identity, winningItem).Select(item => item.Provenance));
        foreach (var candidate in candidates.Skip(1))
            overridden.AddRange(candidate.Items.Select(item => item.Provenance));

        return new ResourceResolutionHandle(identity, winningItem.Read, winningItem.Provenance, overridden);
    }

    public ResourceResolution? Resolve(ResourceIdentity identity)
    {
        var handle = ResolveHandle(identity);
        return handle is null
            ? null
            : new ResourceResolution(identity, handle.ReadBytes(), handle.Winner, handle.Overridden);
    }

    public void Dispose()
    {
        if (!_ownsLayers)
            return;
        foreach (var layer in _layers)
            layer.Dispose();
    }
}
