using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;
using Nwn.Authoring.Areas.Editing;
using Nwn.Authoring.Areas.Tiles;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Editing;
using Nwn.Formats.Tilesets;

namespace Nwn.Authoring.Tests.Areas.Editing;

[TestClass]
public sealed class AreaTileEditorTests
{
    [TestMethod]
    public void ArmedSingleTileCommitsRotationAndUndoAsOneAreaEdit()
    {
        using var documents = CreateSession(width: 2, height: 1, tileIds: [1, 2]);
        var editor = new AreaTileEditor(documents, static _ => null);
        var entry = new TilePaletteEntry("Tile", [7], 1, 1, "tile");

        Assert.IsTrue(editor.Arm(entry));
        Assert.IsTrue(editor.Rotate());
        Assert.AreEqual(1, editor.ArmedOrientation);
        Assert.AreEqual(AreaTileEditOutcome.Changed, editor.CommitAt(0, 0));
        var area = new Nwn.Authoring.Documents.Native.AreDocument(documents.Area.Document);
        Assert.AreEqual(7, AreaTiles.StateAt(area, 0, 0)?.TileId);
        Assert.AreEqual(1, AreaTiles.StateAt(area, 0, 0)?.Orientation);
        Assert.IsTrue(documents.UndoLatest());
        Assert.AreEqual(1, AreaTiles.StateAt(area, 0, 0)?.TileId);
        Assert.AreEqual(0, AreaTiles.StateAt(area, 0, 0)?.Orientation);
    }

    [TestMethod]
    public void OutOfBoundsStampDoesNotMutateOrDirtyArea()
    {
        using var documents = CreateSession(width: 2, height: 1, tileIds: [1, 2]);
        var editor = new AreaTileEditor(documents, static _ => null);
        Assert.IsTrue(editor.Arm(new TilePaletteEntry("Group", [3, 4], 2, 1, "group")));

        Assert.AreEqual(AreaTileEditOutcome.OutOfBounds, editor.CommitAt(1, 0));
        Assert.IsFalse(documents.IsAreaDirty);
        Assert.AreEqual(1, AreaTiles.StateAt(new Nwn.Authoring.Documents.Native.AreDocument(documents.Area.Document), 0, 0)?.TileId);
    }

    [TestMethod]
    public void TileHeightSelectionUsesOneUndoableDocumentMutation()
    {
        using var documents = CreateSession(width: 1, height: 1, tileIds: [5]);
        var editor = new AreaTileEditor(documents, static _ => null);
        editor.SelectCell((0, 0));

        Assert.AreEqual(AreaTileEditOutcome.Changed, editor.AdjustSelectedHeight(1));
        var area = new Nwn.Authoring.Documents.Native.AreDocument(documents.Area.Document);
        Assert.AreEqual(1, AreaTiles.StateAt(area, 0, 0)?.HeightLevel);
        Assert.IsTrue(documents.UndoLatest());
        Assert.AreEqual(0, AreaTiles.StateAt(area, 0, 0)?.HeightLevel);
    }

    private static AreaDocumentEditSession CreateSession(int width, int height, IReadOnlyList<int> tileIds)
    {
        static JsonGffField Int(int value) =>
            JsonGffField.CreateScalar(GffFieldType.Int, Encoding.ASCII.GetBytes(value.ToString()));

        var areaRoot = JsonGffField.CreateStruct(0).Struct!;
        areaRoot.Add("Width", Int(width));
        areaRoot.Add("Height", Int(height));
        areaRoot.Add("Tileset", JsonGffField.CreateScalar(GffFieldType.ResRef, Encoding.UTF8.GetBytes("\"test\"")));
        var tiles = JsonGffField.CreateList();
        for (var i = 0; i < tileIds.Count; i++)
        {
            var tile = JsonGffField.CreateStruct((uint)i).Struct!;
            tile.Add("Tile_ID", Int(tileIds[i]));
            tile.Add("Tile_Orientation", Int(0));
            tile.Add("Tile_Height", Int(0));
            tiles.Elements!.Add(tile);
        }
        areaRoot.Add("Tile_List", tiles);

        static JsonGffDocument CreateSimple(string type, string name)
        {
            var root = JsonGffField.CreateStruct(0).Struct!;
            root.Add(name, JsonGffField.CreateScalar(GffFieldType.CExoString, Encoding.UTF8.GetBytes("\"\"")));
            return new JsonGffDocument(type, root);
        }

        var areaDocument = new JsonGffDocument("ARE ", areaRoot);
        var instanceDocument = CreateSimple("GIT ", "Tag");
        var commentDocument = CreateSimple("GIC ", "Comment");
        return new AreaDocumentEditSession(
            new DocumentSession("area.are", areaDocument),
            new DocumentSession("area.git", instanceDocument),
            new DocumentSession("area.gic", commentDocument));
    }
}
