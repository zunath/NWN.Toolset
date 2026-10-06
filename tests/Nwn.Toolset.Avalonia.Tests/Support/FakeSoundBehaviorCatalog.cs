using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Behaviors;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Sounds;

namespace Nwn.Toolset.Avalonia.Tests.Support;

/// <summary>A sound catalog with a looping behavior, a playlist behavior and the raw behavior.</summary>
internal sealed class FakeSoundBehaviorCatalog : ISoundBehaviorCatalog
{
    private static BehaviorFieldDefinition SoundList(int maxItems) => new()
    {
        Label = "Sounds", Name = SoundBehaviorValueStore.SoundsField, Kind = BehaviorFieldKind.SoundList,
        FieldType = GffFieldType.List, MaxItems = maxItems, IsRequired = true
    };

    public static SoundBehavior Loop { get; } = new()
    {
        Id = "loop",
        DisplayName = "Loop",
        IsLoop = true,
        Fields = [SoundList(1)],
        Manages =
        [
            new BehaviorManagedValue { Label = "Looping", Name = "Looping", FieldType = GffFieldType.Byte, IntValue = 1 },
        ],
    };

    public static SoundBehavior Playlist { get; } = new()
    {
        Id = "playlist",
        DisplayName = "Playlist",
        Fields = [SoundList(8)],
        Manages =
        [
            new BehaviorManagedValue { Label = "Looping", Name = "Looping", FieldType = GffFieldType.Byte, IntValue = 0 },
        ],
    };

    public static SoundBehavior Raw { get; } = new() { Id = "custom", DisplayName = "Custom", AllowsVariables = true };

    public IReadOnlyList<SoundBehavior> All { get; } = [Loop, Playlist, Raw];

    public SoundBehavior Custom => Raw;

    public IReadOnlyList<BehaviorFieldDefinition> BasicFields { get; } =
    [
        new BehaviorFieldDefinition
        {
            Label = "Tag", Name = "Tag", Kind = BehaviorFieldKind.Text, FieldType = GffFieldType.CExoString
        },
    ];

    public SoundBehavior Classify(JsonGffStruct sound) =>
        sound.GetIntOrNull("Looping") == 1 ? Loop : Playlist;
}
