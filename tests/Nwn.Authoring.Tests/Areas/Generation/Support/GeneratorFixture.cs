using Nwn.Authoring.Areas.Generation;
using Nwn.Authoring.Areas.Generation.Composition;
using Nwn.Authoring.Areas.Generation.Drafting;
using Nwn.Authoring.Areas.Generation.Decoration;
using Nwn.Authoring.Areas.Generation.Hosting;
using Nwn.Authoring.Areas.Generation.Tilesets;

namespace Nwn.Authoring.Tests.Areas.Generation.Support;

/// <summary>A game-neutral tileset, theme and catalog the generator tests run against.</summary>
internal static class GeneratorFixture
{
    public const string TilesetKey = "fixture";
    public const string LayoutKey = "rooms";
    public const string ThemeKey = "fixture_theme";
    public const string PropResRef = "fx_prop";
    public const string ExitPlaceableResRef = "fx_exit";
    public const string ExitDoorResRef = "fx_door";
    public const string TreasureResRef = "fx_chest";
    public const string CreatureResRef = "fx_creature";
    public const string BossResRef = "fx_boss";

    public static TilesetModel CreateTileset()
    {
        var model = new TilesetModel
        {
            Resref = TilesetKey,
            DefaultTerrain = "Solid",
            FloorTerrain = "Floor"
        };
        for (var mask = 0; mask < 16; mask++)
        {
            var corners = new string[4];
            for (var slot = 0; slot < corners.Length; slot++)
                corners[slot] = (mask & (1 << slot)) == 0 ? "Solid" : "Floor";
            model.Tiles.Add(new TileRecord
            {
                TileId = mask,
                Corners = corners,
                CornerHeights = [0, 0, 0, 0],
                Edges = ["", "", "", ""]
            });
        }

        return model;
    }

    public static DungeonTilesetProfile CreateTilesetProfile() => new()
    {
        Key = TilesetKey,
        DisplayName = "Fixture tileset",
        TilesetResref = TilesetKey,
        PrimaryOpenTerrain = "Floor"
    };

    public static DungeonLayoutProfile CreateLayoutProfile() => new()
    {
        Key = LayoutKey,
        DisplayName = "Fixture rooms",
        Template = new MacroLayoutParameters
        {
            MinRooms = 2,
            MaxRooms = 3,
            MinRoomCornerSize = 3,
            MaxRoomCornerSize = 5,
            DoorTransitions = false
        }
    };

    public static DungeonDetail CreateTheme() => new()
    {
        ThemeKey = ThemeKey,
        DisplayName = "Fixture theme",
        TilesetProfileKey = TilesetKey,
        LayoutProfileKey = LayoutKey,
        ExitPlaceableResref = ExitPlaceableResRef,
        ExitDisplayName = "Exit",
        ExitDoorResref = ExitDoorResRef,
        TreasurePlaceableResref = TreasureResRef,
        TreasureDisplayName = "Cache",
        DecorationBaseDensity = 0.5,
        Decorations =
        [
            new DungeonDecorationEntry
            {
                Resref = PropResRef,
                Weight = 1,
                Context = DecorationContext.RoomCenter,
                FootprintRadius = 0.6f
            }
        ],
        Tiers =
        {
            [1] = new DungeonTierDetail
            {
                Tier = 1,
                Creatures = [new DungeonCreatureEntry { Resref = CreatureResRef, Weight = 1 }],
                MinCreaturesPerRoom = 1,
                MaxCreaturesPerRoom = 1,
                BossResref = BossResRef,
                TreasureLootTableId = "FIXTURE_TABLE",
                TreasureItemCount = 2
            }
        }
    };

    public static AreaGenerationCatalog CreateCatalog(bool withTheme) => new(
        withTheme ? [CreateTheme()] : [],
        [CreateTilesetProfile()],
        [CreateLayoutProfile()]);

    /// <summary>Knobs for a small dressed area: three to four rooms, no door transitions, and the requested prop style and density.</summary>
    public static LayoutKnobOverrides CreateOverrides(
        DecorationPlacementStyle style = DecorationPlacementStyle.Spacious,
        int densityPercent = 200) => new()
    {
        Style = DungeonLayoutStyle.RoomsAndCorridors,
        MinRooms = 3,
        MaxRooms = 4,
        MinRoomCornerSize = 3,
        MaxRoomCornerSize = 5,
        CorridorWidth = 1,
        LoopFactorPercent = 25,
        OpenFillTargetPercent = 45,
        EntranceCount = 1,
        ExitCount = 1,
        DoorTransitions = false,
        FeatureDensityPercent = 5,
        DecorationPlacementStyle = style,
        DecorationDensityPercent = densityPercent
    };

    public static FixtureTilesetSource CreateTilesetSource(string fingerprint = "") => new(CreateTileset(), fingerprint);
}
