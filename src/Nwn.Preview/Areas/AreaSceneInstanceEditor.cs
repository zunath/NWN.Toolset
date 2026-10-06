using System.Numerics;
using Nwn.Authoring.Areas.Editing;
using Nwn.Authoring.Resources;

namespace Nwn.Preview.Areas;

/// <summary>Applies placed-instance movement and rotation to area documents and their current scene.</summary>
public sealed class AreaSceneInstanceEditor
{
    private readonly AreaInstanceEditor _instances;

    public AreaSceneInstanceEditor(AreaInstanceEditor instances) =>
        _instances = instances ?? throw new ArgumentNullException(nameof(instances));

    public AreaSceneInstanceEditResult Move(AreaScene scene, InstanceMarker marker, Vector3 position)
    {
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentNullException.ThrowIfNull(marker);
        if (!TryIdentity(marker, out var type, out var index))
            return new AreaSceneInstanceEditResult(AreaSceneInstanceEditOutcome.InvalidIdentity);

        var orientation = marker.Orientation;
        if (marker.Kind == InstanceMarkerKind.Door)
        {
            if (scene.NearestEmptyDoorway(position, marker) is not { } anchor)
                return new AreaSceneInstanceEditResult(AreaSceneInstanceEditOutcome.NoAvailableDoorway);
            position = anchor.Position;
            orientation = anchor.Orientation;
        }

        if (!_instances.SetTransform(type, index,
                position.X, position.Y, position.Z, orientation.X, orientation.Y))
            return new AreaSceneInstanceEditResult(AreaSceneInstanceEditOutcome.NoChange);
        return ApplySceneTransform(scene, marker, position, orientation);
    }

    public AreaSceneInstanceEditResult Rotate(AreaScene scene, InstanceMarker marker, Vector2 orientation)
    {
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentNullException.ThrowIfNull(marker);
        if (marker.Kind is InstanceMarkerKind.Door or InstanceMarkerKind.Sound)
            return new AreaSceneInstanceEditResult(AreaSceneInstanceEditOutcome.RotationNotSupported);
        if (!TryIdentity(marker, out var type, out var index))
            return new AreaSceneInstanceEditResult(AreaSceneInstanceEditOutcome.InvalidIdentity);
        if (!_instances.SetOrientation(type, index, orientation.X, orientation.Y))
            return new AreaSceneInstanceEditResult(AreaSceneInstanceEditOutcome.NoChange);
        return ApplySceneTransform(scene, marker, marker.Position, orientation);
    }

    public AreaSceneInstanceEditResult RotateByRadians(AreaScene scene, InstanceMarker marker, float deltaRadians)
    {
        ArgumentNullException.ThrowIfNull(marker);
        if (marker.Kind is InstanceMarkerKind.Door or InstanceMarkerKind.Sound)
            return new AreaSceneInstanceEditResult(AreaSceneInstanceEditOutcome.RotationNotSupported);
        var heading = MathF.Atan2(marker.Orientation.Y, marker.Orientation.X) + deltaRadians;
        return Rotate(scene, marker, new Vector2(MathF.Cos(heading), MathF.Sin(heading)));
    }

    private static AreaSceneInstanceEditResult ApplySceneTransform(
        AreaScene scene,
        InstanceMarker marker,
        Vector3 position,
        Vector2 orientation)
    {
        var replacement = marker.WithTransform(position, orientation);
        var updated = scene.WithInstanceReplaced(marker, replacement);
        return updated == null
            ? new AreaSceneInstanceEditResult(AreaSceneInstanceEditOutcome.SceneRebuildRequired)
            : new AreaSceneInstanceEditResult(AreaSceneInstanceEditOutcome.Changed, updated, replacement);
    }

    private static bool TryIdentity(InstanceMarker marker, out ModuleResourceType type, out int index)
    {
        index = marker.ListIndex;
        type = marker.Kind switch
        {
            InstanceMarkerKind.Creature => ModuleResourceType.Utc,
            InstanceMarkerKind.Door => ModuleResourceType.Utd,
            InstanceMarkerKind.Item => ModuleResourceType.Uti,
            InstanceMarkerKind.Placeable => ModuleResourceType.Utp,
            InstanceMarkerKind.Sound => ModuleResourceType.Uts,
            InstanceMarkerKind.Store => ModuleResourceType.Utm,
            InstanceMarkerKind.Trigger => ModuleResourceType.Utt,
            InstanceMarkerKind.Waypoint => ModuleResourceType.Utw,
            _ => default
        };
        return index >= 0 && marker.Kind <= InstanceMarkerKind.Waypoint;
    }
}
