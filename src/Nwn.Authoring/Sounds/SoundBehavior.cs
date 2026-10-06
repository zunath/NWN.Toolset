using Nwn.Authoring.Behaviors;

namespace Nwn.Authoring.Sounds;

/// <summary>One behavior offered by the ambient-sound editor.</summary>
public sealed class SoundBehavior : IBehaviorDescriptor
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

    /// <summary>True when the behavior plays one looping sound, so its playlist keeps only the first entry.</summary>
    public bool IsLoop { get; init; }
}
