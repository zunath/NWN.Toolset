using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Preview.Plt;

namespace Nwn.Preview.Tests.Plt;

[TestClass]
public sealed class PltDecoderTests
{
    [TestMethod]
    public void Decoder_PreservesShadeAndOpaqueLayerAndReturnsTopDownPixels()
    {
        var fileOrder = new byte[] { 20, 1, 21, 0, 10, 0, 200, 1 };
        var image = PltDecoder.Decode(PltFixtures.Image(2, 2, fileOrder));

        Assert.AreEqual(new PltPixel(10, 0), image.GetPixel(0, 0));
        Assert.AreEqual(new PltPixel(200, 1), image.GetPixel(1, 0));
        Assert.AreEqual(new PltPixel(20, 1), image.GetPixel(0, 1));
        var copied = image.CopyPixelBytes();
        copied[0] = 255;
        Assert.AreEqual((byte)10, image.GetPixel(0, 0).Shade);
    }

    [TestMethod]
    public void Decoder_RejectsBadHeaderVersionTruncationAndLimits()
    {
        var valid = PltFixtures.Image(1, 1, [20, 0]);
        var badSignature = valid.ToArray();
        badSignature[0] = (byte)'X';
        Assert.ThrowsExactly<FormatException>(() => PltDecoder.Decode(badSignature));
        var badVersion = valid.ToArray();
        badVersion[4] = (byte)'2';
        Assert.ThrowsExactly<NotSupportedException>(() => PltDecoder.Decode(badVersion));
        Assert.ThrowsExactly<FormatException>(() => PltDecoder.Decode(valid[..^1]));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => PltDecoder.Decode(valid,
            new Nwn.Preview.Pixels.RasterDecodeOptions { MaximumPixels = 0 }));
    }
}
