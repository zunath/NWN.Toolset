using Nwn.Authoring.Behaviors;

namespace Nwn.Authoring.Waypoints;

/// <summary>One role a waypoint can play and the values that role owns.</summary>
public sealed class WaypointBehavior : IBehaviorDescriptor
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public string? Group { get; init; }

    public string? Tagline { get; init; }

    public string? Summary { get; init; }

    public IReadOnlyList<BehaviorFieldDefinition> Fields { get; init; } =
        Array.Empty<BehaviorFieldDefinition>();

    public IReadOnlyList<BehaviorManagedValue> Manages { get; init; } =
        Array.Empty<BehaviorManagedValue>();

    public bool AllowsVariables { get; init; }

    /// <summary>
    /// The value the catalog's persisted-behavior local carries while this behavior is chosen, or
    /// null when the local must be absent.
    /// </summary>
    public string? PersistedId { get; init; }
}
