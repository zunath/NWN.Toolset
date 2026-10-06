using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Preview.Dds;
using Nwn.Preview.Tga;
using Nwn.Preview.Pixels;

namespace Nwn.Preview.Tests.Tga;

[TestClass]
public sealed class TgaDecoderNwnCompatibilityTests
{
    [TestMethod]
    public void EightBitGrayscaleWithEightAttributeBitsDecodesAsOpaqueIntensity()
    {
        var raw = TgaDecoder.Decode(TgaFixtures.Image(2, 1, 3, 8, 0x08, [19, 242]));
        Assert.AreEqual(new RgbaPixel(19, 19, 19, 255), raw.GetPixel(0, 0));
        Assert.AreEqual(new RgbaPixel(242, 242, 242, 255), raw.GetPixel(1, 0));

        var rle = TgaDecoder.Decode(TgaFixtures.Image(3, 1, 11, 8, 0x08, [0x82, 77]));
        for (var x = 0; x < 3; x++)
            Assert.AreEqual(new RgbaPixel(77, 77, 77, 255), rle.GetPixel(x, 0));
    }

    [TestMethod]
    [DataRow(3, 1)]
    [DataRow(3, 7)]
    [DataRow(3, 9)]
    [DataRow(3, 15)]
    [DataRow(11, 1)]
    [DataRow(11, 7)]
    [DataRow(11, 9)]
    [DataRow(11, 15)]
    public void GrayscaleStillRejectsAttributeCountsOtherThanZeroOrEight(int imageType, int attributeBits)
    {
        var packet = imageType == 11 ? new byte[] { 0, 42 } : new byte[] { 42 };
        Assert.ThrowsExactly<FormatException>(() => TgaDecoder.Decode(
            TgaFixtures.Image(1, 1, (byte)imageType, 8, (byte)attributeBits, packet)));
    }

    [TestMethod]
    public void EightAttributeBitsDoNotRelaxTruncatedRawOrRlePayloadChecks()
    {
        Assert.ThrowsExactly<FormatException>(() => TgaDecoder.Decode(
            TgaFixtures.Image(2, 1, 3, 8, 0x08, [42])));
        Assert.ThrowsExactly<FormatException>(() => TgaDecoder.Decode(
            TgaFixtures.Image(2, 1, 11, 8, 0x08, [0x81])));
        Assert.ThrowsExactly<FormatException>(() => TgaDecoder.Decode(
            TgaFixtures.Image(1, 1, 11, 8, 0x08, [0x81, 42])));
    }
}
