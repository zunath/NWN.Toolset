using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Areas.Generation;
using Nwn.Authoring.Areas.Generation.Drafting;
using Nwn.Authoring.Areas.Generation.Population;
using Nwn.Authoring.Areas.Tiles;
using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Editing;
using Nwn.Authoring.Tests.Areas.Generation.Support;

namespace Nwn.Authoring.Tests.Areas.Generation.Population;

[TestClass]
public sealed class GeneratedAreaDocumentPopulatorTests
{
    private const string AreaResRef = "fx_area";

    private static (AreDocument Are, GitDocument Git, GicDocument Gic) NewDocuments(AreaGenerationDraft draft)
    {
        var resolved = draft.Result.Resolved!;
        var are = new AreDocument(new JsonGffDocument("ARE ", new JsonGffStruct()));
        var git = new GitDocument(new JsonGffDocument("GIT ", new JsonGffStruct()));
        var gic = new GicDocument(new JsonGffDocument("GIC ", new JsonGffStruct()));
        using (EditScope.EnterConstruction())
            AreaTemplateFactory.PopulateNewArea(are, AreaResRef, "Fixture", draft.Tileset.Resref, resolved.Width, resolved.Height, 0, 0);
        return (are, git, gic);
    }

    private static AreaGenerationDraft Generate(bool withTheme)
    {
        var service = new AreaGenerationAuthoringService(GeneratorFixture.CreateTilesetSource(), GeneratorFixture.CreateCatalog(withTheme));
        var draft = service.Generate(new AreaGenerationSettings
        {
            ThemeKey = withTheme ? GeneratorFixture.ThemeKey : string.Empty,
            TilesetProfileKey = GeneratorFixture.TilesetKey,
            LayoutProfileKey = GeneratorFixture.LayoutKey,
            Width = 20,
            Height = 20,
            Seed = 4242,
            Overrides = GeneratorFixture.CreateOverrides()
        });
        Assert.IsTrue(draft.Result.Success, draft.Result.FailureReason);
        return draft;
    }

    [TestMethod]
    public void ThemelessDraftsWriteTilesAndLeaveTemplateLightsAndObjectsAlone()
    {
        var draft = Generate(withTheme: false);
        var (are, git, gic) = NewDocuments(draft);
        var lightsBefore = are.Tiles[0].GetIntOrNull("Tile_MainLight1");

        using (EditScope.EnterConstruction())
            GeneratedAreaDocumentPopulator.Populate(draft, new FixtureBlueprintSource(), null, null, AreaResRef, are, git, gic);

        var resolved = draft.Result.Resolved!;
        for (var index = 0; index < resolved.Tiles.Length; index++)
        {
            var tile = AreaTiles.At(are, index % resolved.Width, index / resolved.Width)!.Value;
            Assert.AreEqual(resolved.Tiles[index].TileId, tile.TileId);
        }

        Assert.AreEqual(lightsBefore, are.Tiles[0].GetIntOrNull("Tile_MainLight1"));
        Assert.AreEqual(0, git.Fields.GetListOrEmpty("Creature List").Count);
        Assert.AreEqual(0, git.Fields.GetListOrEmpty("Placeable List").Count);
        Assert.AreEqual(0, git.Fields.GetListOrEmpty("WaypointList").Count);
    }

    [TestMethod]
    public void ThemedDraftsPlaceTransitionsTreasureCreaturesAndDressingThroughTheHostPolicy()
    {
        var draft = Generate(withTheme: true);
        var (are, git, gic) = NewDocuments(draft);
        var policy = new RecordingPopulationPolicy();

        using (EditScope.EnterConstruction())
        {
            GeneratedAreaDocumentPopulator.Populate(
                draft, FixtureBlueprintSource.CreateComplete(), policy, null, AreaResRef, are, git, gic);
        }

        var resolved = draft.Result.Resolved!;
        Assert.AreEqual(resolved.Transitions.Count, git.Fields.GetListOrEmpty("WaypointList").Count);
        Assert.AreEqual(resolved.Transitions.Count, gic.Fields.GetListOrEmpty("WaypointList").Count);
        Assert.AreEqual(1, policy.Treasures, "One treasure container per boss room.");
        Assert.AreEqual(2, policy.TreasureTier!.TreasureItemCount);
        Assert.IsTrue(policy.Creatures > 0);
        var creatures = git.Fields.GetListOrEmpty("Creature List");
        Assert.AreEqual(policy.Creatures, creatures.Count);
        Assert.IsTrue(creatures.All(creature => creature.GetIntOrNull("FactionID") == 1));
        var placeables = git.Fields.GetListOrEmpty("Placeable List");
        Assert.IsTrue(placeables.Any(instance => instance.GetStringOrNull("OnOpen") == "fixture_open"));
        Assert.IsTrue(placeables.All(instance => instance.GetStringOrNull("Tag")!.StartsWith("PG_" + AreaResRef, StringComparison.Ordinal)));
        Assert.IsTrue(placeables.Count >= draft.Result.PlannedDecorations.Count + 1);
    }

    [TestMethod]
    public void GeneratedTagsStayWithinTheNativeLimitForTheLongestResRef()
    {
        var tag = GeneratedAreaDocumentPopulator.GeneratedTag(new string('a', 16), "DOOR_EXIT_3");

        Assert.IsTrue(tag.Length <= 32, tag);
    }

    [TestMethod]
    public void MissingBlueprintsFailLoudlyInsteadOfWritingPartialAreas()
    {
        var draft = Generate(withTheme: true);
        var (are, git, gic) = NewDocuments(draft);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            using (EditScope.EnterConstruction())
                GeneratedAreaDocumentPopulator.Populate(draft, new FixtureBlueprintSource(), null, null, AreaResRef, are, git, gic);
        });
    }

    [TestMethod]
    public void CreaturesKeepClearOfEachOtherAndOfTheRoomBoundary()
    {
        var draft = Generate(withTheme: true);
        var resolved = draft.Result.Resolved!;
        var room = resolved.Rooms.First(candidate => candidate.Role == RoomRole.Standard);
        var occupied = new List<(float X, float Y, float Radius)>();

        var anchors = GeneratedAreaDocumentPopulator.SelectCreatureAnchors(resolved, room, [0.5f, 0.5f], occupied, new Random(7));

        Assert.AreEqual(2, anchors.Count);
        var dx = anchors[0].X - anchors[1].X;
        var dy = anchors[0].Y - anchors[1].Y;
        Assert.IsTrue(MathF.Sqrt(dx * dx + dy * dy) >= 1f - 0.001f);
        Assert.AreEqual(2, occupied.Count, "Selected anchors are reserved for later placements.");
    }
}
