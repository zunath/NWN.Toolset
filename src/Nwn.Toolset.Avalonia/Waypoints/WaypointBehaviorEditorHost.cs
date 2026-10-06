using Nwn.Authoring.Waypoints;
using Nwn.Toolset.Avalonia.Behaviors;

namespace Nwn.Toolset.Avalonia.Waypoints;

/// <summary>The game data and services a waypoint behavior editor takes from its host.</summary>
public sealed class WaypointBehaviorEditorHost : BehaviorEditorHost
{
    /// <summary>The host's waypoint behaviors, Basic rows and destination rules.</summary>
    public required IWaypointBehaviorCatalog Catalog { get; init; }
}
