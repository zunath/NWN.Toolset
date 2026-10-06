using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Areas.Generation;
using Nwn.Authoring.Areas.Generation.Tilesets;

namespace Nwn.Authoring.Tests.Areas.Generation.Grids;

[TestClass]
public sealed class EdgeCrosserGridTests
{
    [TestMethod]
    public void NeighboringCellsAddressOneSharedEdge()
    {
        var grid = new EdgeCrosserGrid(2, 1);

        grid.SetEdge(0, 0, EdgeSlot.Right, "Corridor");
        Assert.AreEqual("Corridor", grid.GetEdge(1, 0, EdgeSlot.Left));

        grid.SetEdge(1, 0, EdgeSlot.Left, "Doorway");
        Assert.AreEqual("Doorway", grid.GetEdge(0, 0, EdgeSlot.Right));
    }

    [TestMethod]
    public void BorderEdgesUseTheirOwnSharedStorageAndNullClearsToBlank()
    {
        var grid = new EdgeCrosserGrid(1, 1);

        grid.SetEdge(0, 0, EdgeSlot.Left, "West");
        grid.SetEdge(0, 0, EdgeSlot.Right, "East");
        grid.SetEdge(0, 0, EdgeSlot.Bottom, "South");
        grid.SetEdge(0, 0, EdgeSlot.Top, "North");

        Assert.AreEqual("West", grid.GetEdge(0, 0, EdgeSlot.Left));
        Assert.AreEqual("East", grid.GetEdge(0, 0, EdgeSlot.Right));
        Assert.AreEqual("South", grid.GetEdge(0, 0, EdgeSlot.Bottom));
        Assert.AreEqual("North", grid.GetEdge(0, 0, EdgeSlot.Top));

        grid.SetEdge(0, 0, EdgeSlot.Top, null);
        Assert.AreEqual(string.Empty, grid.GetEdge(0, 0, EdgeSlot.Top));
    }
}