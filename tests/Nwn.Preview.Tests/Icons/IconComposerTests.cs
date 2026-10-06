using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Preview.Icons;
using Nwn.Preview.Thumbnails;

namespace Nwn.Preview.Tests.Icons;

[TestClass]
public sealed class IconComposerTests
{
    private static ThumbnailTexture Solid(int width, int height, byte r, byte g, byte b, byte a)
    {
        var pixels = new byte[width * height * 4];
        for (var i = 0; i < pixels.Length; i += 4)
        {
            pixels[i] = r;
            pixels[i + 1] = g;
            pixels[i + 2] = b;
            pixels[i + 3] = a;
        }
        return new(width, height, pixels);
    }
    private static (byte B, byte G, byte R, byte A) PixelAt(IconImage image, int x, int y)
    {
        var offset = (y * image.Width + x) * IconImage.BytesPerPixel;
        return (image.Bgra[offset], image.Bgra[offset + 1], image.Bgra[offset + 2], image.Bgra[offset + 3]);
    }

    [TestMethod]
    public void No_Layers_Composes_Nothing() => Assert.IsNull(IconComposer.Compose(Array.Empty<ThumbnailTexture>()));
    [TestMethod]
    public void Rgba_Source_Becomes_Bgra_Output()
    {
        var image = IconComposer.Compose([Solid(2, 2, 10, 20, 30, 255)])!;
        Assert.AreEqual(((byte)30, (byte)20, (byte)10, (byte)255), PixelAt(image, 0, 0));
    }

    [TestMethod]
    public void The_Canvas_Is_As_Large_As_The_Largest_Layer()
    {
        var image = IconComposer.Compose([Solid(8, 4, 1, 1, 1, 255), Solid(2, 16, 2, 2, 2, 255)])!;
        Assert.AreEqual(8, image.Width);
        Assert.AreEqual(16, image.Height);
    }

    [TestMethod]
    public void Later_Layers_Paint_Over_Earlier_Ones()
    {
        var image = IconComposer.Compose([Solid(2, 2, 255, 0, 0, 255), Solid(2, 2, 0, 255, 0, 255)])!;
        Assert.AreEqual(((byte)0, (byte)255, (byte)0, (byte)255), PixelAt(image, 1, 1));
    }

    [TestMethod]
    public void A_Fully_Transparent_Layer_Leaves_What_Is_Underneath_Alone()
    {
        var image = IconComposer.Compose([Solid(2, 2, 255, 0, 0, 255), Solid(2, 2, 0, 0, 255, 0)])!;
        Assert.AreEqual(((byte)0, (byte)0, (byte)255, (byte)255), PixelAt(image, 0, 1));
    }

    [TestMethod]
    public void A_Half_Transparent_Layer_Blends_Towards_Its_Own_Colour()
    {
        var image = IconComposer.Compose([Solid(2, 2, 0, 0, 0, 255), Solid(2, 2, 255, 255, 255, 128)])!;
        var pixel = PixelAt(image, 0, 0);
        Assert.AreEqual(255, pixel.A);
        Assert.IsTrue(pixel.R is >= 120 and <= 136);
    }

    [TestMethod]
    public void Nothing_Is_Drawn_Where_Every_Layer_Is_Transparent()
    {
        var image = IconComposer.Compose([Solid(2, 2, 9, 9, 9, 0)])!;
        Assert.AreEqual(0, image.Bgra[3]);
    }

    [TestMethod]
    public void Degenerate_And_Truncated_Layers_Are_Skipped_Rather_Than_Fatal()
    {
        var truncated = new ThumbnailTexture(4, 4, new byte[8]);
        Assert.IsNull(IconComposer.Compose([truncated]));
        Assert.AreEqual(2, IconComposer.Compose([truncated, Solid(2, 2, 1, 2, 3, 255)])!.Width);
    }
}