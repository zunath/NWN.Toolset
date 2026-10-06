namespace Nwn.Preview.Areas;

/// <summary>Updated scene state or the reason an in-place update could not be completed.</summary>
public sealed record AreaSceneInstanceEditResult(
    AreaSceneInstanceEditOutcome Outcome,
    AreaScene? Scene = null,
    InstanceMarker? UpdatedMarker = null);
