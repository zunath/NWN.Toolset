using Nwn.Authoring.Behaviors;
using Nwn.Authoring.Documents.NimGff;

namespace Nwn.Authoring.Waypoints;

/// <summary>A host's waypoint behaviors, the rows every behavior shares, and its destination rules.</summary>
public interface IWaypointBehaviorCatalog
{
    /// <summary>Every behavior, in rail order.</summary>
    IReadOnlyList<WaypointBehavior> All { get; }

    /// <summary>The fixed Basic rows shown for every behavior.</summary>
    IReadOnlyList<BehaviorFieldDefinition> BasicFields { get; }

    /// <summary>
    /// The local that records a chosen behavior the content alone cannot reveal, or null when the
    /// host persists no behavior.
    /// </summary>
    string? PersistedBehaviorLocal { get; }

    /// <summary>The behavior an existing waypoint already plays.</summary>
    WaypointBehavior Classify(JsonGffStruct waypoint);

    /// <summary>True when only one placed waypoint in the module may carry <paramref name="tag"/>.</summary>
    bool IsSingletonDestinationTag(string? tag);
}
