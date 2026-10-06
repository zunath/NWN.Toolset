using Nwn.Authoring.Behaviors;
using Nwn.Authoring.Triggers;
using Nwn.Toolset.Avalonia.Behaviors;

namespace Nwn.Toolset.Avalonia.Triggers;

/// <summary>The game data and services a trigger behavior editor takes from its host.</summary>
public sealed class TriggerBehaviorEditorHost : BehaviorEditorHost
{
    /// <summary>The host's trigger behaviors and Basic rows.</summary>
    public required ITriggerBehaviorCatalog Catalog { get; init; }

    /// <summary>
    /// Says what a destination tag reaches: found, missing, ambiguous, of the wrong type, unverifiable or
    /// without a destination type. Destination rows print no status when null.
    /// </summary>
    public TransitionDestinationResolver? ResolveDestination { get; init; }
}
