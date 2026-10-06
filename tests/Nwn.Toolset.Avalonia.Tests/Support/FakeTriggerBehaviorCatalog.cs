using Nwn.Authoring.Behaviors;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Triggers;

namespace Nwn.Toolset.Avalonia.Tests.Support;

/// <summary>A trigger catalog with an area transition, a trap and the raw behavior.</summary>
internal sealed class FakeTriggerBehaviorCatalog : ITriggerBehaviorCatalog
{
    public static TriggerBehavior Transition { get; } = new()
    {
        Id = "transition",
        DisplayName = "Area Transition",
        Group = "Movement",
        Fields =
        [
            new BehaviorFieldDefinition
            {
                Label = "Destination tag", Name = "LinkedTo", Kind = BehaviorFieldKind.TagReference,
                FieldType = GffFieldType.CExoString, IsRequired = true, TagScope = BehaviorTagScope.WaypointOrDoor
            },
            new BehaviorFieldDefinition
            {
                Label = "Destination is a", Name = "LinkedToFlags", Kind = BehaviorFieldKind.Choice,
                FieldType = GffFieldType.Byte,
                Choices = [new BehaviorChoice(1, "Door"), new BehaviorChoice(2, "Waypoint")]
            },
        ],
        Manages =
        [
            new BehaviorManagedValue { Label = "Trigger Type", Name = "Type", FieldType = GffFieldType.Int, IntValue = 1 },
        ],
    };

    public static TriggerBehavior Trap { get; } = new()
    {
        Id = "trap",
        DisplayName = "Trap",
        Group = "Hazard",
        Fields =
        [
            new BehaviorFieldDefinition
            {
                Label = "Detect DC", Name = "TrapDetectDC", Kind = BehaviorFieldKind.Integer,
                FieldType = GffFieldType.Byte
            },
        ],
        Manages =
        [
            new BehaviorManagedValue { Label = "Trigger Type", Name = "Type", FieldType = GffFieldType.Int, IntValue = 2 },
            new BehaviorManagedValue { Label = "Trap Flag", Name = "TrapFlag", FieldType = GffFieldType.Byte, IntValue = 1 },
        ],
    };

    public static TriggerBehavior Raw { get; } = new()
    {
        Id = "custom",
        DisplayName = "Custom",
        AllowsVariables = true,
        Fields =
        [
            new BehaviorFieldDefinition
            {
                Label = "OnEnter", Name = "ScriptOnEnter", Kind = BehaviorFieldKind.Script,
                FieldType = GffFieldType.ResRef
            },
            new BehaviorFieldDefinition
            {
                Label = "Trapped", Name = "TrapFlag", Kind = BehaviorFieldKind.Check, FieldType = GffFieldType.Byte
            },
            new TriggerFieldDefinition
            {
                Label = "Trap script", Name = "OnTrapTriggered", Kind = BehaviorFieldKind.Script,
                FieldType = GffFieldType.ResRef, IsRequired = true, VisibleWhenField = "TrapFlag"
            },
        ],
    };

    public IReadOnlyList<TriggerBehavior> All { get; } = [Transition, Trap, Raw];

    public TriggerBehavior Custom => Raw;

    public IReadOnlyList<BehaviorFieldDefinition> BasicFields { get; } =
    [
        new BehaviorFieldDefinition
        {
            Label = "Tag", Name = "Tag", Kind = BehaviorFieldKind.Text, FieldType = GffFieldType.CExoString
        },
    ];

    public TriggerBehavior Classify(JsonGffStruct trigger) => TriggerKindReader.Read(trigger) switch
    {
        TriggerKind.AreaTransition => Transition,
        TriggerKind.Trap => Trap,
        _ => Raw,
    };
}
