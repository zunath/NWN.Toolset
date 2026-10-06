using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Areas.Generation.Tilesets;

namespace Nwn.Authoring.Tests.Areas.Generation.Tilesets;

[TestClass]
public sealed class TilesetSetParserTests
{
    [TestMethod]
    public void ParseMapsNativeTileDoorAndGroupDataAndKeepsRotationSlots()
    {
        const string contents = """
            [GENERAL]
            Name=Fixture
            Interior=1
            HasHeightTransition=1
            Transition=3
            Border=Border
            Default=Solid
            Floor=Floor

            [TERRAIN0]
            Name=Floor
            [CROSSER0]
            Name=Doorway

            [TILE0]
            Model=fixture
            WalkMesh=fixture
            TopLeft=NorthWest
            TopLeftHeight=1
            TopRight=NorthEast
            TopRightHeight=2
            BottomRight=SouthEast
            BottomRightHeight=3
            BottomLeft=SouthWest
            BottomLeftHeight=4
            Top=Doorway
            Right=
            Bottom=
            Left=
            Doors=99
            ImageMap2D=fixturemap

            [TILE0DOOR0]
            Type=17
            X=-4.5
            Y=2.25
            Z=1.5
            Orientation=90

            [GROUP0]
            Name=OneTile
            Rows=1
            Columns=1
            Tile0=0
            """;

        var model = TilesetSetParser.Parse("fixture", contents);

        Assert.AreEqual("fixture", model.Resref);
        Assert.AreEqual("Fixture", model.Name);
        Assert.IsTrue(model.IsInterior);
        Assert.IsTrue(model.HasHeightTransition);
        Assert.AreEqual(3f, model.HeightTransition);
        Assert.AreEqual("Border", model.BorderTerrain);
        Assert.AreEqual("Solid", model.DefaultTerrain);
        Assert.AreEqual("Floor", model.FloorTerrain);
        Assert.AreEqual(1, model.Tiles.Count);
        Assert.AreEqual(1, model.Groups.Count);
        Assert.AreEqual("OneTile", model.Groups[0].Name);
        CollectionAssert.AreEqual(new[] { 0 }, model.Groups[0].TileIds);

        var tile = model.Tiles[0];
        Assert.AreEqual(0, tile.GroupIndex);
        Assert.AreEqual("fixturemap", tile.ImageMap2D);
        Assert.AreEqual("NorthEast", tile.GetCornerAt(1, CornerSlot.TopLeft));
        Assert.AreEqual(2, tile.GetCornerHeightAt(1, CornerSlot.TopLeft));
        Assert.AreEqual("Doorway", tile.GetEdgeAt(0, EdgeSlot.Top));
        Assert.IsTrue(tile.HasAnyCrosser);
        Assert.AreEqual(1, tile.Doors.Count);
        Assert.AreEqual(17, tile.Doors[0].Type);
        Assert.AreEqual(-4.5f, tile.Doors[0].X);
        Assert.AreEqual(2.25f, tile.Doors[0].Y);
        Assert.AreEqual(1.5f, tile.Doors[0].Z);
        Assert.AreEqual(90f, tile.Doors[0].Orientation);
    }
}
