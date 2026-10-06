using Nwn.Authoring.Behaviors;

namespace Nwn.Authoring.Doors;

/// <summary>One role a door can play and the values that role owns.</summary>
public sealed class DoorBehavior : IBehaviorDescriptor
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public string? Group { get; init; }

    public string? Tagline { get; init; }

    public string? Summary { get; init; }

    public IReadOnlyList<DoorFieldDefinition> Fields { get; init; } = Array.Empty<DoorFieldDefinition>();

    public IReadOnlyList<BehaviorManagedValue> Manages { get; init; } = Array.Empty<BehaviorManagedValue>();

    public IReadOnlyList<string> OwnedLocalPrefixes { get; init; } = Array.Empty<string>();

    /// <summary>
    /// True only for the raw behavior: the local-variable table is the builder's to edit, and
    /// leaving the behavior sweeps every local it holds.
    /// </summary>
    public bool AllowsVariables { get; init; }

    /// <summary>How choosing this behavior derives the native KeyRequired flag.</summary>
    public DoorKeyRequiredRule KeyRequiredRule { get; init; }
}
