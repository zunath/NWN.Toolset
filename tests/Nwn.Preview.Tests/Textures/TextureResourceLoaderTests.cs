using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Formats.Resources;
using Nwn.Preview.Dds;
using Nwn.Preview.Pixels;
using Nwn.Preview.Textures;
using Nwn.Preview.Tests.Plt;

namespace Nwn.Preview.Tests.Textures;

[TestClass]
public sealed class TextureResourceLoaderTests
{
    [TestMethod]
    public void Load_CompositesEachPltLayerThroughItsHostSelectedPalette()
    {
        var plt = PltFixtures.Image(2, 1, [128, 0, 128, 1]);
        var palettePixels = new byte[256 * 4];
        for (var column = 0; column < 256; column++)
        {
            palettePixels[column * 4] = (byte)column;
            palettePixels[column * 4 + 3] = 255;
        }
        var red = new RgbaImage(256, 1, palettePixels);
        Array.Clear(palettePixels);
        for (var column = 0; column < 256; column++)
        {
            palettePixels[column * 4 + 1] = (byte)column;
            palettePixels[column * 4 + 3] = 200;
        }
        var green = new RgbaImage(256, 1, palettePixels);
        var request = new TextureLoadRequest
        {
            ResolvePalette = layer => layer == 0 ? red : green,
            PaletteRows = new Dictionary<byte, int> { [0] = 0, [1] = 0 }
        };

        var loaded = TextureResourceLoader.Load("armor", (_, type) =>
            type == ResourceType.Plt ? plt : null, request);

        Assert.IsNotNull(loaded);
        Assert.AreEqual(TextureSourceFormat.Plt, loaded.SourceFormat);
        Assert.AreEqual(new RgbaPixel(128, 0, 0, 255), loaded.Image.GetPixel(0, 0));
        Assert.AreEqual(new RgbaPixel(0, 128, 0, 200), loaded.Image.GetPixel(1, 0));
    }

    [TestMethod]
    public void Load_RejectsOversizedCompressedResourceBeforeDecoderAllocation()
    {
        var request = new TextureLoadRequest { MaximumCompressedBytes = 1 };
        Assert.ThrowsExactly<FormatException>(() => TextureResourceLoader.Load("large", (_, type) =>
            type == ResourceType.Dds ? new byte[2] : null, request));
    }
}
