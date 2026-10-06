using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Areas.Generation.Tilesets;

namespace Nwn.Authoring.Tests.Areas.Generation.Tilesets;

[TestClass]
public sealed class TileCandidateIndexTests
{
    [TestMethod]
    public void BuildPreservesTileAndOrientationInventoryOrder()
    {
        var model = new TilesetModel();
        model.Tiles.Add(CreateTile(0, "A"));
        var second = CreateTile(1, "A");
        second.PathNode = "B";
        model.Tiles.Add(second);

        var index = TileCandidateIndex.Build(model, heightAware: false);
        var key = TileCandidateIndex.CreateKey("A", "A", "A", "A", "", "", "", "");

        Assert.IsTrue(index.TryGetCandidates(key, out var candidates));
        CollectionAssert.AreEqual(
            new[] { (0, 0, 0), (0, 1, 0), (0, 2, 0), (0, 3, 0), (1, 0, 0), (1, 1, 0), (1, 2, 0), (1, 3, 0) },
            candidates.All.ToArray());
        Assert.AreEqual(4, candidates.FullyPathable.Count);
    }

    [TestMethod]
    public void LegacyIndexExcludesRaisedTilesAndHeightIndexMatchesNormalizedProfile()
    {
        var model = new TilesetModel();
        model.Tiles.Add(CreateTile(0, "Floor"));
        var raised = CreateTile(1, "Floor");
        raised.CornerHeights = [2, 3, 2, 3];
        model.Tiles.Add(raised);

        var legacyKey = TileCandidateIndex.CreateKey("Floor", "Floor", "Floor", "Floor", "", "", "", "");
        var heightKey = TileCandidateIndex.CreateHeightAwareKey(
            "Floor", "Floor", "Floor", "Floor", "", "", "", "", 0, 1, 0, 1);
        var legacy = TileCandidateIndex.Build(model, heightAware: false);
        var heightAware = TileCandidateIndex.Build(model, heightAware: true);

        Assert.IsTrue(legacy.TryGetCandidates(legacyKey, out var legacyCandidates));
        Assert.AreEqual(4, legacyCandidates.All.Count);
        Assert.IsTrue(heightAware.TryGetCandidates(heightKey, out var candidates));
        CollectionAssert.AreEqual(new[] { (1, 0, 2), (1, 2, 2) }, candidates.All.ToArray());
    }

    [TestMethod]
    public void BuildExcludesGroupsExcludedIdsAndUnmatchedDoorSlots()
    {
        var model = new TilesetModel();
        model.Tiles.Add(CreateTile(0, "Floor"));
        var excluded = CreateTile(1, "Floor");
        excluded.Corners = ["Other", "Other", "Other", "Other"];
        model.Tiles.Add(excluded);
        var grouped = CreateTile(2, "Floor");
        grouped.GroupIndex = 0;
        model.Tiles.Add(grouped);
        var bareDoor = CreateTile(3, "Floor");
        bareDoor.Doors.Add(new TileDoorRecord());
        model.Tiles.Add(bareDoor);
        var alternateDoor = CreateTile(4, "Floor");
        alternateDoor.Edges = ["sideDoor", "", "", ""];
        alternateDoor.Doors.Add(new TileDoorRecord());
        model.Tiles.Add(alternateDoor);
        var canonicalDoor = CreateTile(5, "Floor");
        canonicalDoor.Edges = ["Doorway", "", "", ""];
        canonicalDoor.Doors.Add(new TileDoorRecord());
        model.Tiles.Add(canonicalDoor);

        var blankKey = TileCandidateIndex.CreateKey("Floor", "Floor", "Floor", "Floor", "", "", "", "");
        var noAlternate = TileCandidateIndex.Build(model, false, excludedTiles: [1]);
        var withAlternate = TileCandidateIndex.Build(model, false, ["sidedoor"], [1]);

        Assert.IsTrue(noAlternate.TryGetCandidates(blankKey, out var candidates));
        CollectionAssert.AreEqual(new[] { (0, 0, 0), (0, 1, 0), (0, 2, 0), (0, 3, 0) }, candidates.All.ToArray());
        Assert.IsTrue(withAlternate.TryGetCandidates(blankKey, out var alternateBlankCandidates));
        Assert.AreEqual(4, alternateBlankCandidates.All.Count);
        var alternateKey = TileCandidateIndex.CreateKey(
            "Floor", "Floor", "Floor", "Floor", "sideDoor", "", "", "");
        Assert.IsTrue(withAlternate.TryGetCandidates(alternateKey, out var alternateCandidates));
        CollectionAssert.AreEqual(new[] { (4, 0, 0) }, alternateCandidates.All.ToArray());
        var canonicalKey = TileCandidateIndex.CreateKey(
            "Floor", "Floor", "Floor", "Floor", "Doorway", "", "", "");
        Assert.IsTrue(withAlternate.TryGetCandidates(canonicalKey, out var canonicalCandidates));
        CollectionAssert.AreEqual(new[] { (5, 0, 0) }, canonicalCandidates.All.ToArray());
    }

    [TestMethod]
    public void SignatureIsCaseInsensitiveAndHeightProfileIsPartOfKey()
    {
        var lower = TileCandidateIndex.CreateKey("floor", "wall", "floor", "wall", "", "Doorway", "", "");
        var upper = TileCandidateIndex.CreateKey("FLOOR", "WALL", "FLOOR", "WALL", "", "doorway", "", "");
        var flat = TileCandidateIndex.CreateHeightAwareKey("floor", "wall", "floor", "wall", "", "", "", "", 0, 0, 0, 0);
        var slope = TileCandidateIndex.CreateHeightAwareKey("floor", "wall", "floor", "wall", "", "", "", "", 0, 1, 0, 1);

        Assert.AreEqual(lower, upper);
        Assert.AreNotEqual(flat, slope);
    }

    private static TileRecord CreateTile(int id, string terrain)
    {
        return new TileRecord
        {
            TileId = id,
            PathNode = "A",
            Corners = [terrain, terrain, terrain, terrain],
            CornerHeights = [0, 0, 0, 0],
            Edges = ["", "", "", ""]
        };
    }
}
