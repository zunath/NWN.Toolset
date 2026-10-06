using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Areas.Generation;
using Nwn.Authoring.Areas.Generation.Geometry;
using Nwn.Authoring.Areas.Generation.Tilesets;

namespace Nwn.Authoring.Tests.Areas.Generation.Geometry;

[TestClass]
public sealed class TileDoorGeometryTests
{
    [TestMethod]
    public void RotationUsesExactQuarterTurnSwapsForNegativeAndPositiveOrientations()
    {
        Assert.AreEqual((1f, 2f), TileDoorGeometry.RotateCcw90Multiple(1f, 2f, 0));
        Assert.AreEqual((-2f, 1f), TileDoorGeometry.RotateCcw90Multiple(1f, 2f, 1));
        Assert.AreEqual((-1f, -2f), TileDoorGeometry.RotateCcw90Multiple(1f, 2f, 2));
        Assert.AreEqual((2f, -1f), TileDoorGeometry.RotateCcw90Multiple(1f, 2f, 3));
        Assert.AreEqual((2f, -1f), TileDoorGeometry.RotateCcw90Multiple(1f, 2f, -1));
        Assert.AreEqual((-2f, 1f), TileDoorGeometry.RotateCcw90Multiple(1f, 2f, 5));
    }

    [TestMethod]
    public void DoorWorldTransformAppliesCellCenterRotationAndNormalizedBearing()
    {
        var slot = new TileDoorRecord
        {
            X = 2f,
            Y = -1f,
            Z = 3.5f,
            Orientation = 135f
        };

        var world = TileDoorGeometry.DoorWorldTransform(slot, 2, 3, 1);

        Assert.AreEqual(26f, world.X, 0.001f);
        Assert.AreEqual(37f, world.Y, 0.001f);
        Assert.AreEqual(3.5f, world.Z, 0.001f);
        Assert.AreEqual(-135f, world.Orientation, 0.001f);
        Assert.AreEqual(180f, TileDoorGeometry.NormalizeDegrees(-180f), 0.001f);
    }

    [TestMethod]
    public void CellCornerAndFlatnessHelpersUseSouthOriginGridCoordinates()
    {
        var corners = new CornerTerrainGrid(1, 1, "Unused");
        corners.Labels[0, 0] = "SW";
        corners.Labels[1, 0] = "SE";
        corners.Labels[0, 1] = "NW";
        corners.Labels[1, 1] = "NE";

        Assert.AreEqual(("NW", "NE", "SE", "SW"), TileDoorGeometry.CellCorners(corners, 0, 0));
        Assert.IsTrue(TileDoorGeometry.IsFlatCell(corners, 0, 0));
        corners.Heights[1, 1] = 2;
        Assert.IsFalse(TileDoorGeometry.IsFlatCell(corners, 0, 0));
        Assert.IsTrue(TileDoorGeometry.Eq("Ground", "ground"));
        Assert.IsFalse(TileDoorGeometry.Eq("Ground", "Wall"));
    }

    [TestMethod]
    public void CrosserDetectionChecksAllFourCellEdges()
    {
        var crossers = new EdgeCrosserGrid(1, 1);

        Assert.IsFalse(TileDoorGeometry.HasAnyCrosserEdge(crossers, (0, 0)));
        crossers.SetEdge(0, 0, EdgeSlot.Top, "Doorway");
        Assert.IsTrue(TileDoorGeometry.HasAnyCrosserEdge(crossers, (0, 0)));
    }
}