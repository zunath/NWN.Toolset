using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Behaviors;
using Nwn.Authoring.Doors;
using Nwn.Authoring.Documents.NimGff;

namespace Nwn.Toolset.Avalonia.Tests.Support;

/// <summary>A door catalog with a locked behavior, an area transition and the raw behavior.</summary>
internal sealed class FakeDoorBehaviorCatalog : IDoorBehaviorCatalog
{
    public static DoorBehavior Locked { get; } = new()
    {
        Id = "locked",
        DisplayName = "Locked Door",
        Group = "Access",
        KeyRequiredRule = DoorKeyRequiredRule.FromKeyTag,
        Fields =
        [
            new DoorFieldDefinition
            {
                Label = "Opens with key", Name = "KeyName", Kind = BehaviorFieldKind.TagReference,
                FieldType = GffFieldType.CExoString, TagScope = BehaviorTagScope.Item,
                NonEmptySetsField = "KeyRequired"
            },
        ],
        Manages =
        [
            new BehaviorManagedValue { Label = "Locked", Name = "Locked", FieldType = GffFieldType.Byte, IntValue = 1 },
        ],
    };

    public static DoorBehavior Transition { get; } = new()
    {
        Id = "transition",
        DisplayName = "Area Transition",
        Group = "Movement",
        KeyRequiredRule = DoorKeyRequiredRule.FromKeyTagWhenLocked,
        Fields =
        [
            new DoorFieldDefinition
            {
                Label = "Destination", Name = "LinkedTo", Kind = BehaviorFieldKind.TagReference,
                FieldType = GffFieldType.CExoString, IsRequired = true, TagScope = BehaviorTagScope.WaypointOrDoor
            },
            new DoorFieldDefinition
            {
                Label = "Locked", Name = "Locked", Kind = BehaviorFieldKind.Check, FieldType = GffFieldType.Byte
            },
            new DoorFieldDefinition
            {
                Label = "Pick lock DC", Name = "OpenLockDC", Kind = BehaviorFieldKind.Integer,
                FieldType = GffFieldType.Byte, VisibleWhenField = "Locked"
            },
        ],
    };

    public static DoorBehavior Raw { get; } = new()
    {
        Id = "custom",
        DisplayName = "Custom",
        AllowsVariables = true,
        Fields =
        [
            new DoorFieldDefinition
            {
                Label = "Conversation", Name = "Conversation", Kind = BehaviorFieldKind.Text, FieldType = GffFieldType.ResRef
            },
        ],
    };

    public IReadOnlyList<DoorBehavior> All { get; } = [Locked, Transition, Raw];

    public DoorBehavior Custom => Raw;

    public IReadOnlyList<DoorFieldDefinition> BasicFields { get; } =
    [
        new DoorFieldDefinition
        {
            Label = "Tag", Name = "Tag", Kind = BehaviorFieldKind.Text, FieldType = GffFieldType.CExoString
        },
        new DoorFieldDefinition
        {
            Label = "Faction", Name = "Faction", Kind = BehaviorFieldKind.Choice, FieldType = GffFieldType.Dword,
            ChoicesKey = "factions"
        },
    ];

    public DoorScriptConventions Conventions { get; } =
        new("closer", ["closer", "other_closer"], "death", "KEY_");

    public DoorBehavior Classify(JsonGffStruct door)
    {
        if (!string.IsNullOrWhiteSpace(door.GetStringOrNull("LinkedTo")))
            return Transition;

        return door.GetIntOrNull("Locked") == 1 ? Locked : Raw;
    }
}
