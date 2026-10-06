using Nwn.Authoring.Behaviors;

namespace Nwn.Authoring.Triggers;

/// <summary>A behavior field that a trigger's own values can hide, such as a trap row on a trigger that is no trap.</summary>
public sealed class TriggerFieldDefinition : BehaviorFieldDefinition
{
    /// <summary>The row appears only while this integer field equals <see cref="VisibleWhenValue"/>.</summary>
    public string? VisibleWhenField { get; init; }

    /// <summary>The value <see cref="VisibleWhenField"/> must hold for the row to appear.</summary>
    public long VisibleWhenValue { get; init; } = 1;
}
