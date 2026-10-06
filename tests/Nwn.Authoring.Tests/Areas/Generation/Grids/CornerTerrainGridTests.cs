using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Areas.Generation;

namespace Nwn.Authoring.Tests.Areas.Generation.Grids;

[TestClass]
public sealed class CornerTerrainGridTests
{
    [TestMethod]
    public void ConstructorInitializesCornerLabelsAndHeightPlane()
    {
        var grid = new CornerTerrainGrid(2, 1, "Ground");

        Assert.AreEqual(2, grid.Width);
        Assert.AreEqual(1, grid.Height);
        Assert.AreEqual(3, grid.Labels.GetLength(0));
        Assert.AreEqual(2, grid.Labels.GetLength(1));
        Assert.AreEqual(3, grid.Heights.GetLength(0));
        Assert.AreEqual(2, grid.Heights.GetLength(1));
        for (var x = 0; x <= grid.Width; x++)
        for (var y = 0; y <= grid.Height; y++)
        {
            Assert.AreEqual("Ground", grid.Labels[x, y]);
            Assert.AreEqual(0, grid.Heights[x, y]);
        }
        Assert.IsFalse(grid.HasAnyHeight());
    }

    [TestMethod]
    public void HasAnyHeightChecksEveryCornerAndResetsWhenValuesReturnToZero()
    {
        var grid = new CornerTerrainGrid(2, 2, "Ground");
        grid.Heights[0, 0] = -1;
        Assert.IsTrue(grid.HasAnyHeight());

        grid.Heights[0, 0] = 0;
        grid.Heights[2, 2] = 3;
        Assert.IsTrue(grid.HasAnyHeight());

        grid.Heights[2, 2] = 0;
        Assert.IsFalse(grid.HasAnyHeight());
    }
}