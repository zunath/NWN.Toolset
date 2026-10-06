namespace Nwn.Toolset.Avalonia.Areas;

/// <summary>Host-owned scene state that drives the viewport's retained notices and actions.</summary>
public sealed record AreaSceneOverlay
{
    public bool IsBuildingScene { get; init; }
    public string SceneStatus { get; init; } = string.Empty;
    public bool HasSceneSelection { get; init; }
    public bool HasTileSelection { get; init; }
    public string TileSelectionStatus { get; init; } = string.Empty;
    public string PlacementStatus { get; init; } = string.Empty;
    public bool CanRotateSelection { get; init; }
    public bool HasSceneStatus => !string.IsNullOrEmpty(SceneStatus);
    public bool HasPlacementStatus => !string.IsNullOrEmpty(PlacementStatus);
    public bool HasViewportHud => !IsBuildingScene &&
        (HasSceneStatus || HasSceneSelection || HasTileSelection || HasPlacementStatus);
}
