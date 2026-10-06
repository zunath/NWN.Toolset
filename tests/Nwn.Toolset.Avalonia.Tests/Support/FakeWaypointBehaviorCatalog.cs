using Nwn.Authoring.Behaviors;
using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Waypoints;

namespace Nwn.Toolset.Avalonia.Tests.Support;

/// <summary>A waypoint catalog with a persisted destination behavior, a map note and the raw behavior.</summary>
internal sealed class FakeWaypointBehaviorCatalog : IWaypointBehaviorCatalog
{
    public const string Local = "TOOLSET_BEHAVIOR";

    public static WaypointBehavior Destination { get; } = new()
    {
        Id = "destination",
        DisplayName = "Destination",
        PersistedId = "destination",
        Fields =
        [
            new BehaviorFieldDefinition
            {
                Label = "Destination tag", Name = "Tag", Kind = BehaviorFieldKind.Text,
                FieldType = GffFieldType.CExoString, IsRequired = true
            },
        ],
    };

    public static WaypointBehavior MapNote { get; } = new()
    {
        Id = "map_note",
        DisplayName = "Map Note",
        Fields =
        [
            new BehaviorFieldDefinition
            {
                Label = "Note", Name = "MapNote", Kind = BehaviorFieldKind.LocalizedText,
                FieldType = GffFieldType.CExoLocString
            },
        ],
        Manages =
        [
            new BehaviorManagedValue { Label = "Has map note", Name = "HasMapNote", FieldType = GffFieldType.Byte, IntValue = 1 },
        ],
    };

    public static WaypointBehavior Raw { get; } = new() { Id = "custom", DisplayName = "Custom", AllowsVariables = true };

    public IReadOnlyList<WaypointBehavior> All { get; } = [Destination, MapNote, Raw];

    public IReadOnlyList<BehaviorFieldDefinition> BasicFields { get; } =
    [
        new BehaviorFieldDefinition
        {
            Label = "ResRef", Name = "TemplateResRef", Kind = BehaviorFieldKind.Text, FieldType = GffFieldType.ResRef
        },
    ];

    public string? PersistedBehaviorLocal => Local;

    public WaypointBehavior Classify(JsonGffStruct waypoint)
    {
        if (new VarTable(waypoint).GetString(Local) == Destination.PersistedId)
            return Destination;

        return waypoint.GetIntOrNull("HasMapNote") == 1 ? MapNote : Raw;
    }

    public bool IsSingletonDestinationTag(string? tag) =>
        string.Equals(tag, "UNIQUE_DESTINATION", StringComparison.OrdinalIgnoreCase);
}
