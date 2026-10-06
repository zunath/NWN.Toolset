namespace Nwn.Authoring.Areas.Editing;

/// <summary>Result of applying an area tile editing operation.</summary>
public enum AreaTileEditOutcome
{
    Changed,
    NoChange,
    Rejected,
    MissingTileset,
    OutOfBounds
}
