using Nwn.Authoring.Behaviors;

namespace Nwn.Authoring.Triggers;

/// <summary>One role a trigger can play and the values that role owns.</summary>
/// <remarks>
/// Local variables are reachable only under <see cref="AllowsVariables"/>, which is true for the raw
/// behavior alone. Every other behavior owns whichever locals it needs and exposes them as named fields,
/// so there is never a second place to set the same thing.
/// </remarks>
public sealed class TriggerBehavior : IBehaviorDescriptor
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public string? Group { get; init; }

    public string? Tagline { get; init; }

    public string? Summary { get; init; }

    public IReadOnlyList<BehaviorFieldDefinition> Fields { get; init; } = Array.Empty<BehaviorFieldDefinition>();

    public IReadOnlyList<BehaviorManagedValue> Manages { get; init; } = Array.Empty<BehaviorManagedValue>();

    public bool AllowsVariables { get; init; }

    /// <summary>Every local this behavior owns, whether as a row or as a managed value.</summary>
    public IEnumerable<string> OwnedLocals =>
        Fields.Where(row => row.Storage == BehaviorFieldStorage.Local).Select(row => row.Name)
            .Concat(Manages.Where(value => value.Storage == BehaviorFieldStorage.Local).Select(value => value.Name))
            .Distinct(StringComparer.Ordinal);
}
