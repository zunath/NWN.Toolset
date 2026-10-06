using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Preview.Pixels;
using Nwn.Preview.Plt;
using Nwn.Preview.Tint;

namespace Nwn.Preview.Tests.Tint;

[TestClass]
public sealed class PackedTintMaskDecoderTests
{
    [TestMethod]
    public void LayerBinsRetainAllBoundaryValuesShadeAndTopDownOrientation()
    {
        var green = new byte[] { 0, 25, 26, 127, 128, 229, 230, 255 };
        var pixels = green.SelectMany((value, index) => new byte[] { (byte)(10 + index), value, 250, 255 }).ToArray();
        var mask = PackedTintMaskDecoder.Decode(new RgbaImage(4, 2, pixels), 10);
        CollectionAssert.AreEqual(new byte[] { 10, 0, 11, 0, 12, 1, 13, 4, 14, 5, 15, 8, 16, 9, 17, 9 }, mask.CopyPixelBytes());
        Assert.AreEqual(new PltPixel(14, 5), mask.GetPixel(0, 1));
        var fullRange = PackedTintMaskDecoder.Decode(new RgbaImage(1, 1, [128, 255, 0, 255]), 256);
        Assert.AreEqual(new PltPixel(128, 255), fullRange.GetPixel(0, 0));
    }

    [TestMethod]
    public void CallerBoundsAndLayerCountAreCheckedBeforeAllocatingTheOutput()
    {
        var mask = new RgbaImage(2, 2, new byte[16]);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => PackedTintMaskDecoder.Decode(mask, 0));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => PackedTintMaskDecoder.Decode(mask, 257));
        Assert.ThrowsExactly<FormatException>(() => PackedTintMaskDecoder.Decode(mask, 10, new() { MaximumPixels = 1 }));
        Assert.ThrowsExactly<FormatException>(() => PackedTintMaskDecoder.Decode(mask, 10, new() { MaximumWidth = 1 }));
    }
}
