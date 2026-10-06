using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Preview.Icons;
using System.Numerics;

namespace Nwn.Preview.Tests.Icons;

[TestClass]
public sealed class IconCanvasTests
{
    private const uint OpaqueWhite = 0xFFFFFFFF;
    private static byte AlphaAt(IconImage image, int x, int y) => image.Bgra[(y * image.Width + x) * IconImage.BytesPerPixel + 3];

    [TestMethod]
    public void A_Filled_Square_Covers_Its_Interior()
    {
        var canvas = new IconCanvas(32, 32);
        canvas.FillPolygon([new(8, 8), new(24, 8), new(24, 24), new(8, 24)], OpaqueWhite);
        Assert.AreEqual(255, AlphaAt(canvas.ToImage(), 16, 16));
    }

    [TestMethod]
    public void A_Filled_Square_Leaves_The_Rest_Of_The_Canvas_Transparent()
    {
        var canvas = new IconCanvas(32, 32);
        canvas.FillPolygon([new(8, 8), new(24, 8), new(24, 24), new(8, 24)], OpaqueWhite);
        var image = canvas.ToImage();
        Assert.AreEqual(0, AlphaAt(image, 0, 0));
        Assert.AreEqual(0, AlphaAt(image, 31, 31));
    }

    [TestMethod]
    public void A_Diagonal_Edge_Is_Anti_Aliased_Rather_Than_Stepped()
    {
        var canvas = new IconCanvas(32, 32);
        canvas.FillPolygon([new(2, 2), new(30, 2), new(2, 30)], OpaqueWhite);
        var partial = 0;
        var image = canvas.ToImage();
        for (var y = 0; y < 32; y++)
        {
            for (var x = 0; x < 32; x++)
            {
                var alpha = AlphaAt(image, x, y);
                if (alpha is > 0 and < 255)
                {
                    partial++;
                }
            }
        }
        Assert.IsTrue(partial > 4, "the hypotenuse should produce partially covered pixels");
    }

    [TestMethod]
    public void A_Degenerate_Polygon_Draws_Nothing_And_Does_Not_Throw()
    {
        var canvas = new IconCanvas(8, 8);
        canvas.FillPolygon([new(1, 1), new(2, 2)], OpaqueWhite);
        CollectionAssert.AreEqual(new byte[8 * 8 * 4], canvas.ToImage().Bgra);
    }

    [TestMethod]
    public void Shapes_Outside_The_Canvas_Are_Clipped_Rather_Than_Fatal()
    {
        var canvas = new IconCanvas(8, 8);
        canvas.FillCircle(new(-40, -40), 5, OpaqueWhite);
        canvas.FillPolygon([new(100, 100), new(140, 100), new(140, 140)], OpaqueWhite);
        canvas.StrokeLine(new(-20, 4), new(60, 4), 2, OpaqueWhite);
    }

    [TestMethod]
    public void A_Stroked_Line_Marks_The_Pixels_It_Passes_Through()
    {
        var canvas = new IconCanvas(32, 32);
        canvas.StrokeLine(new(4, 16), new(28, 16), thickness: 4, OpaqueWhite);
        var image = canvas.ToImage();
        Assert.AreEqual(255, AlphaAt(image, 16, 16));
        Assert.AreEqual(0, AlphaAt(image, 16, 2));
    }

    [TestMethod]
    public void An_Ellipse_Is_Wider_Than_It_Is_Tall_When_Told_To_Be()
    {
        var canvas = new IconCanvas(64, 64);
        canvas.FillEllipse(new(32, 32), radiusX: 28, radiusY: 6, OpaqueWhite);
        var image = canvas.ToImage();
        Assert.AreEqual(255, AlphaAt(image, 58, 32));
        Assert.AreEqual(0, AlphaAt(image, 32, 58));
    }

    [TestMethod]
    public void Translucent_Paint_Accumulates_Towards_Opaque()
    {
        var canvas = new IconCanvas(8, 8);
        Vector2[] square = [new(0, 0), new(8, 0), new(8, 8), new(0, 8)];
        canvas.FillPolygon(square, 0x80FFFFFF);
        var one = AlphaAt(canvas.ToImage(), 4, 4);
        canvas.FillPolygon(square, 0x80FFFFFF);
        Assert.IsTrue(AlphaAt(canvas.ToImage(), 4, 4) > one);
    }

    [TestMethod]
    public void A_Non_Positive_Canvas_Is_Rejected() => Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new IconCanvas(0, 8));
}