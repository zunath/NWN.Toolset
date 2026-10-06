using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Preview.Dds;
using Nwn.Preview.Pixels;
using Nwn.Preview.Plt;

namespace Nwn.Preview.Tests.Plt;

[TestClass]
public sealed class PltCompositorTests
{
    [TestMethod]
    public void Compose_UsesSelectedLayerRowsShadeColumnAndPaletteAlpha()
    {
        var fileOrder = new byte[] { 20, 1, 21, 0, 10, 0, 200, 1 };
        var texture = PltDecoder.Decode(PltFixtures.Image(2, 2, fileOrder));
        var palette = CreatePalette();

        var output = PltCompositor.Compose(texture, palette, new Dictionary<byte, int> { [0] = 0, [1] = 1 });

        Assert.AreEqual(new RgbaPixel(10, 0, 100, 255), output.GetPixel(0, 0));
        Assert.AreEqual(new RgbaPixel(0, 200, 200, 128), output.GetPixel(1, 0));
        Assert.AreEqual(new RgbaPixel(0, 20, 200, 128), output.GetPixel(0, 1));
        Assert.AreEqual(new RgbaPixel(21, 0, 100, 255), output.GetPixel(1, 1));
    }

    [TestMethod]
    public void Compose_RequiresExplicitKnownLayerRowsAnd256ColumnPalette()
    {
        var texture = PltDecoder.Decode(PltFixtures.Image(1, 1, [30, 9]));
        var palette = CreatePalette();
        Assert.ThrowsExactly<FormatException>(() => PltCompositor.Compose(texture, palette, new Dictionary<byte, int>()));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => PltCompositor.Compose(texture, palette,
            new Dictionary<byte, int> { [9] = 2 }));
        Assert.ThrowsExactly<ArgumentException>(() => PltCompositor.Compose(texture,
            new RgbaImage(1, 1, [0, 0, 0, 255]), new Dictionary<byte, int> { [9] = 0 }));
        var largerTexture = PltDecoder.Decode(PltFixtures.Image(2, 2, [1, 9, 2, 9, 3, 9, 4, 9]));
        Assert.ThrowsExactly<FormatException>(() => PltCompositor.Compose(largerTexture, palette,
            new Dictionary<byte, int> { [9] = 0 }, new RasterDecodeOptions { MaximumPixels = 1 }));
    }

    [TestMethod]
    public void ExactColorUsesCallerNeutralRampMiddleShadeAndClampsHighlights()
    {
        var texture = PltDecoder.Decode(PltFixtures.Image(4, 1, [0, 0, 128, 0, 255, 0, 128, 1]));
        var bytes = new byte[256 * 2 * 4];
        for (var x = 0; x < 256; x++)
        {
            var offset = x * 4;
            bytes[offset] = bytes[offset + 1] = bytes[offset + 2] = (byte)(x / 2 + (x == 255 ? 1 : 0));
            bytes[offset + 3] = 255;
        }
        var colors = new Dictionary<byte, PltLayerTint>
        {
            [0] = new(0, new(100, 200, 50, 99)),
            [1] = new(1, new(100, 200, 50, 88)),
        };
        var image = PltCompositor.Compose(texture, new RgbaImage(256, 2, bytes), colors);
        Assert.AreEqual(new RgbaPixel(0, 0, 0, 99), image.GetPixel(0, 0));
        Assert.AreEqual(new RgbaPixel(100, 200, 50, 99), image.GetPixel(1, 0));
        Assert.AreEqual(new RgbaPixel(200, 255, 100, 99), image.GetPixel(2, 0));
        Assert.AreEqual(new RgbaPixel(0, 0, 0, 88), image.GetPixel(3, 0));
    }

    [TestMethod]
    public void ExactAndPaletteLayersCanCoexistWithoutReplacingPaletteAlpha()
    {
        var texture = PltDecoder.Decode(PltFixtures.Image(2, 1, [128, 0, 200, 1]));
        var image = PltCompositor.Compose(texture, CreatePalette(), new Dictionary<byte, PltLayerTint>
        {
            [0] = new(0, new(10, 20, 30, 40)),
            [1] = new(1),
        });
        Assert.AreEqual(new RgbaPixel(10, 20, 30, 40), image.GetPixel(0, 0));
        Assert.AreEqual(new RgbaPixel(0, 200, 200, 128), image.GetPixel(1, 0));
    }

    private static RgbaImage CreatePalette()
    {
        var pixels = new byte[256 * 2 * 4];
        for (var x = 0; x < 256; x++)
        {
            var first = x * 4;
            pixels[first] = (byte)x;
            pixels[first + 2] = 100;
            pixels[first + 3] = 255;
            var second = (256 + x) * 4;
            pixels[second + 1] = (byte)x;
            pixels[second + 2] = 200;
            pixels[second + 3] = 128;
        }
        return new RgbaImage(256, 2, pixels);
    }
}
