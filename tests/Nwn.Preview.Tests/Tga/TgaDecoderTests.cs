using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Preview.Dds;
using Nwn.Preview.Tga;

namespace Nwn.Preview.Tests.Tga;

[TestClass]
public sealed class TgaDecoderTests
{
    [TestMethod]
    public void TrueColor_ConvertsBgraAndBothOriginBitsToTopDownRgba()
    {
        var payload = new byte[]
        {
            0, 0, 255, 11, 0, 255, 0, 22,
            255, 0, 0, 33, 255, 255, 255, 44
        };
        var image = TgaDecoder.Decode(TgaFixtures.Image(2, 2, 2, 32, 0x18, payload));

        Assert.AreEqual(new RgbaPixel(255, 255, 255, 44), image.GetPixel(0, 0));
        Assert.AreEqual(new RgbaPixel(0, 0, 255, 33), image.GetPixel(1, 0));
        Assert.AreEqual(new RgbaPixel(0, 255, 0, 22), image.GetPixel(0, 1));
        Assert.AreEqual(new RgbaPixel(255, 0, 0, 11), image.GetPixel(1, 1));
    }

    [TestMethod]
    public void GrayscaleRle_ExpandsPixelsAndRejectsTruncatedOrOversizedPackets()
    {
        var image = TgaDecoder.Decode(TgaFixtures.Image(3, 1, 11, 8, 0, [0x82, 77]));
        Assert.AreEqual(new RgbaPixel(77, 77, 77, 255), image.GetPixel(2, 0));

        Assert.ThrowsExactly<FormatException>(() => TgaDecoder.Decode(TgaFixtures.Image(3, 1, 11, 8, 0, [0x82])));
        Assert.ThrowsExactly<FormatException>(() => TgaDecoder.Decode(TgaFixtures.Image(2, 1, 11, 8, 0, [0x82, 77])));
    }

    [TestMethod]
    public void Decoder_RejectsUnsupportedPaletteAndAllocationCases()
    {
        Assert.ThrowsExactly<NotSupportedException>(() => TgaDecoder.Decode(TgaFixtures.Image(1, 1, 1, 8, 0, [0])));
        var custom = new Nwn.Preview.Pixels.RasterDecodeOptions { MaximumPixels = 1 };
        Assert.ThrowsExactly<FormatException>(() => TgaDecoder.Decode(TgaFixtures.Image(2, 1, 2, 24, 0, new byte[6]), custom));
    }
}
