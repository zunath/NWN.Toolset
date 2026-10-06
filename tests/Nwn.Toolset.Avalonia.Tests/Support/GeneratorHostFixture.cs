using Nwn.Authoring.Areas.Generation;
using Nwn.Authoring.Areas.Generation.Composition;
using Nwn.Authoring.Areas.Generation.Hosting;
using Nwn.Toolset.Avalonia.Areas.Generation;

namespace Nwn.Toolset.Avalonia.Tests.Support;

/// <summary>Builds an Area Generator host over fixture content: one tileset, one layout profile, no themes.</summary>
public sealed class GeneratorHostFixture
{
    public RecordingGeneratedAreaWriter Writer { get; } = new();

    public InlineBackgroundTaskRunner Tasks { get; } = new();

    public AreaGeneratorHost CreateHost(AreaGeneratorWindowOptions? window = null) => new(
        new AreaGenerationCatalog(
            [],
            [new DungeonTilesetProfile
            {
                Key = GeneratorTilesetSource.ResRef,
                DisplayName = "Fixture tileset",
                TilesetResref = GeneratorTilesetSource.ResRef,
                PrimaryOpenTerrain = "Floor",
                Lighting = null
            }],
            [new DungeonLayoutProfile
            {
                Key = "rooms",
                DisplayName = "Fixture rooms",
                Template = new MacroLayoutParameters
                {
                    MinRooms = 2,
                    MaxRooms = 3,
                    MinRoomCornerSize = 3,
                    MaxRoomCornerSize = 5,
                    DoorTransitions = false
                }
            }]),
        new GeneratorTilesetSource(),
        Writer)
    {
        BackgroundTasks = Tasks,
        Window = window
    };
}
