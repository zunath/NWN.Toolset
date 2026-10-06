namespace Nwn.Toolset.Avalonia.Areas;

/// <summary>Host-resolved, neutral appearance inputs associated with one render mesh.</summary>
public sealed class AreaViewportMeshMetadata
{
    private readonly IReadOnlyDictionary<int, int> _layerColorIndices;

    public IReadOnlyDictionary<int, int> LayerColorIndices => _layerColorIndices;
    public bool UsesItemTintOverrides { get; }

    public AreaViewportMeshMetadata(
        IReadOnlyDictionary<int, int>? layerColorIndices = null,
        bool usesItemTintOverrides = false)
    {
        _layerColorIndices = new System.Collections.ObjectModel.ReadOnlyDictionary<int, int>(
            layerColorIndices is null
                ? new Dictionary<int, int>()
                : new Dictionary<int, int>(layerColorIndices));
        UsesItemTintOverrides = usesItemTintOverrides;
    }
}
