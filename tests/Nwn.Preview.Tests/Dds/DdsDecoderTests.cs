using System.Buffers.Binary;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Preview.Dds;

namespace Nwn.Preview.Tests.Dds;

[TestClass]
public sealed class DdsDecoderTests
{
    [TestMethod]
    public void Bc1_DecodesEndpointsInterpolationAndTransparentMode()
    {
        var endpoints = new byte[] { 0x00, 0xF8, 0x1F, 0x00, 0, 0, 0, 0 };
        var red = DdsDecoder.Decode(DdsFixtures.Compressed(4, 4, "DXT1", endpoints));
        Assert.AreEqual(new RgbaPixel(255, 0, 0, 255), red.GetPixel(3, 3));

        BinaryPrimitives.WriteUInt32LittleEndian(endpoints.AsSpan(4), 0xAAAAAAAA);
        var mixed = DdsDecoder.Decode(DdsFixtures.Compressed(4, 4, "DXT1", endpoints));
        Assert.AreEqual(new RgbaPixel(170, 0, 85, 255), mixed.GetPixel(0, 0));

        var transparent = new byte[] { 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
        var image = DdsDecoder.Decode(DdsFixtures.Compressed(4, 4, "DXT1", transparent));
        Assert.AreEqual((byte)0, image.GetPixel(0, 0).A);
    }

    [TestMethod]
    public void Bc2_ExpandsExplicitNibblesAndBc3InterpolatesAlpha()
    {
        var bc2 = new byte[16];
        bc2[0] = 0xF0;
        bc2[8] = 0x00;
        bc2[9] = 0xF8;
        bc2[10] = 0x1F;
        var dxt3 = DdsDecoder.Decode(DdsFixtures.Compressed(4, 4, "DXT3", bc2));
        Assert.AreEqual((byte)0, dxt3.GetPixel(0, 0).A);
        Assert.AreEqual((byte)255, dxt3.GetPixel(1, 0).A);

        var bc3 = new byte[16];
        bc3[0] = 255;
        bc3[1] = 0;
        bc3[2] = 2;
        bc3[8] = 0x00;
        bc3[9] = 0xF8;
        bc3[10] = 0x1F;
        var dxt5 = DdsDecoder.Decode(DdsFixtures.Compressed(4, 4, "DXT5", bc3));
        Assert.AreEqual((byte)218, dxt5.GetPixel(0, 0).A);
        Assert.AreEqual(DdsPixelFormat.Bc3, dxt5.Format);
    }

    [TestMethod]
    public void CompactBioWare_DecodesDxt1AndDxt5WithCompleteMipChains()
    {
        var dxt1 = new byte[24];
        dxt1[0] = 0x00;
        dxt1[1] = 0xF8;
        var bc1 = DdsDecoder.Decode(DdsFixtures.CompactBioWare(4, 4, 3, dxt1));
        Assert.AreEqual(new RgbaPixel(255, 0, 0, 255), bc1.GetPixel(0, 0));
        Assert.AreEqual(3, bc1.MipCount);

        var dxt5 = new byte[48];
        dxt5[0] = 255;
        dxt5[1] = 0;
        dxt5[8] = 0x00;
        dxt5[9] = 0xF8;
        var bc3 = DdsDecoder.Decode(DdsFixtures.CompactBioWare(4, 4, 4, dxt5));
        Assert.AreEqual((byte)255, bc3.GetPixel(0, 0).A);
        Assert.AreEqual(DdsPixelFormat.Bc3, bc3.Format);
    }

    [TestMethod]
    public void CompactBioWare_RejectsInvalidFormatSizeTruncationAndAllocation()
    {
        Assert.ThrowsExactly<NotSupportedException>(() => DdsDecoder.Decode(
            DdsFixtures.CompactBioWare(4, 4, 5, new byte[48])));
        Assert.ThrowsExactly<FormatException>(() => DdsDecoder.Decode(
            DdsFixtures.CompactBioWare(4, 4, 3, new byte[24], baseSize: 7)));
        Assert.ThrowsExactly<FormatException>(() => DdsDecoder.Decode(
            DdsFixtures.CompactBioWare(4, 4, 3, new byte[23])));
        Assert.ThrowsExactly<FormatException>(() => DdsDecoder.Decode(
            DdsFixtures.CompactBioWare(4, 4, 3, new byte[24]), new DdsDecodeOptions { MaximumPixels = 8 }));
    }

    [TestMethod]
    public void MaskedRgbAndRgba_ExpandChannelsAndDefaultMissingAlpha()
    {
        var rgba = DdsDecoder.Decode(DdsFixtures.Rgb32([0xAA, 0x33, 0x22, 0x11],
            0x000000FF, 0x0000FF00, 0x00FF0000, 0xFF000000));
        Assert.AreEqual(new RgbaPixel(0xAA, 0x33, 0x22, 0x11), rgba.GetPixel(0, 0));

        var rgb = DdsDecoder.Decode(DdsFixtures.Rgb24([0x11, 0x22, 0x33], 0x000000FF, 0x0000FF00, 0x00FF0000));
        Assert.AreEqual(new RgbaPixel(0x11, 0x22, 0x33, 255), rgb.GetPixel(0, 0));
    }

    [TestMethod]
    public void Decoder_ValidatesMipChainAndTruncationBeforeDecoding()
    {
        var allLevels = Enumerable.Repeat((byte)0, 24).ToArray();
        var mipmapped = DdsDecoder.Decode(DdsFixtures.Compressed(4, 4, "DXT1", allLevels, mipCount: 3));
        Assert.AreEqual(3, mipmapped.MipCount);
        Assert.ThrowsExactly<FormatException>(() => DdsDecoder.Decode(
            DdsFixtures.Compressed(4, 4, "DXT1", allLevels[..^1], mipCount: 3)));
        Assert.ThrowsExactly<FormatException>(() => DdsDecoder.Decode(
            DdsFixtures.Compressed(4, 4, "DXT1", new byte[8], mipCount: 4)));
    }

    [TestMethod]
    public void Decoder_RejectsTruncatedHeadersMaliciousDimensionsAndBadMasks()
    {
        Assert.ThrowsExactly<FormatException>(() => DdsDecoder.Decode(new byte[127]));
        var oversized = DdsFixtures.Header(int.MaxValue, 1, 1);
        Encoding.ASCII.GetBytes("DXT1").CopyTo(oversized, 84);
        BinaryPrimitives.WriteUInt32LittleEndian(oversized.AsSpan(80, 4), 0x4);
        Assert.ThrowsExactly<FormatException>(() => DdsDecoder.Decode(oversized));

        var badMasks = DdsFixtures.Rgb32([1, 2, 3, 4], 0xFF, 0xFF, 0x00FF0000, 0xFF000000);
        Assert.ThrowsExactly<FormatException>(() => DdsDecoder.Decode(badMasks));
    }

    [TestMethod]
    public void Decoder_ReportsUnsupportedFourCcClearly()
    {
        var unsupported = DdsFixtures.Compressed(4, 4, "DXT2", new byte[16]);
        var error = Assert.ThrowsExactly<NotSupportedException>(() => DdsDecoder.Decode(unsupported));
        StringAssert.Contains(error.Message, "DXT2");
    }

    [TestMethod]
    public void UnsignedBc5KeepsIndependentChannelsAndBothInterpolationModes()
    {
        var block = new byte[16];
        block[0] = 255;
        block[9] = 255;
        ulong redIndices = 0;
        ulong greenIndices = 0;
        for (var pixel = 0; pixel < 16; pixel++)
        {
            redIndices |= (ulong)(pixel % 8) << (pixel * 3);
            greenIndices |= (ulong)(7 - pixel % 8) << (pixel * 3);
        }
        for (var index = 0; index < 6; index++)
        {
            block[2 + index] = (byte)(redIndices >> (index * 8));
            block[10 + index] = (byte)(greenIndices >> (index * 8));
        }
        var bytes = DdsFixtures.Compressed(4, 4, "ATI2", block);
        var image = DdsDecoder.Decode(bytes);
        byte[] red = [255, 0, 218, 182, 145, 109, 72, 36];
        byte[] green = [0, 255, 51, 102, 153, 204, 0, 255];
        Assert.AreEqual(DdsPixelFormat.Bc5, image.Format);
        for (var pixel = 0; pixel < 16; pixel++)
            Assert.AreEqual(new RgbaPixel(red[pixel % 8], green[7 - pixel % 8], 0, 255), image.GetPixel(pixel % 4, pixel / 4));
        var reversed = DdsDecoder.Decode(bytes, new DdsDecodeOptions { StoredRowOrder = DdsStoredRowOrder.BottomUp });
        Assert.AreEqual(image.GetPixel(2, 3), reversed.GetPixel(2, 0));
        var cropped = DdsDecoder.Decode(DdsFixtures.Compressed(3, 2, "ATI2", block));
        Assert.AreEqual(image.GetPixel(2, 1), cropped.GetPixel(2, 1));
    }

    [TestMethod]
    public void Bc5ValidatesCompleteMipPayloadAndBoundsBeforeAllocation()
    {
        Assert.AreEqual(3, DdsDecoder.Decode(DdsFixtures.Compressed(4, 4, "ATI2", new byte[48], 3)).MipCount);
        Assert.ThrowsExactly<FormatException>(() => DdsDecoder.Decode(DdsFixtures.Compressed(4, 4, "ATI2", new byte[47], 3)));
        Assert.ThrowsExactly<FormatException>(() => DdsDecoder.Decode(DdsFixtures.Compressed(4, 4, "ATI2", new byte[15])));
        Assert.ThrowsExactly<FormatException>(() => DdsDecoder.Decode(DdsFixtures.Compressed(4, 4, "ATI2", new byte[16]),
            new DdsDecodeOptions { MaximumPixels = 8 }));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => DdsDecoder.Decode(DdsFixtures.Compressed(4, 4, "ATI2", new byte[16]),
            new DdsDecodeOptions { StoredRowOrder = (DdsStoredRowOrder)99 }));
    }

    [TestMethod]
    public void CompactBioWareDefaultsToTopDownOutputWithAnExplicitStoredRowOverride()
    {
        var payload = new byte[40];
        payload[1] = 0xF8;
        payload[8] = 0x1F;
        var bytes = DdsFixtures.CompactBioWare(4, 8, 3, payload);
        var normalized = DdsDecoder.Decode(bytes);
        Assert.AreEqual(new RgbaPixel(0, 0, 255, 255), normalized.GetPixel(0, 0));
        Assert.AreEqual(new RgbaPixel(255, 0, 0, 255), normalized.GetPixel(0, 7));
        var asTopDown = DdsDecoder.Decode(bytes, new DdsDecodeOptions { StoredRowOrder = DdsStoredRowOrder.TopDown });
        Assert.AreEqual(new RgbaPixel(255, 0, 0, 255), asTopDown.GetPixel(0, 0));
    }
}
