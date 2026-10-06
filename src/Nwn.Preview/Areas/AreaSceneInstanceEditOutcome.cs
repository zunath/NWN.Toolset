namespace Nwn.Preview.Areas;

/// <summary>Result of a transform edit applied to an instance represented in an area scene.</summary>
public enum AreaSceneInstanceEditOutcome
{
    Changed,
    NoChange,
    InvalidIdentity,
    NoAvailableDoorway,
    RotationNotSupported,
    SceneRebuildRequired
}
