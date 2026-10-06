using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Preview.Icons;
using Nwn.Preview.Thumbnails;
using Nwn.Authoring.Resources;

namespace Nwn.Preview.Tests.Icons;

[TestClass]
public sealed class IconRenderingTests
{
    [TestMethod]
    public void CanvasFillsShapeAndLeavesOutsideTransparent()
    {
        var canvas = new IconCanvas(16, 16);
        canvas.FillPolygon([new(2, 2), new(14, 2), new(14, 14), new(2, 14)], 0xFFFFFFFF);
        var image = canvas.ToImage();
        Assert.AreEqual(255, image.Bgra[(8 * image.Width + 8) * IconImage.BytesPerPixel + 3]);
        Assert.AreEqual(0, image.Bgra[3]);
    }

    [TestMethod]
    public void IconComposerConvertsRgbaLayersToBgraAndCompositesInOrder()
    {
        var image = IconComposer.Compose(new[]
        {
            new ThumbnailTexture(1, 1, [255, 0, 0, 255]),
            new ThumbnailTexture(1, 1, [0, 0, 255, 255])
        });
        Assert.IsNotNull(image);
        CollectionAssert.AreEqual(new byte[] { 255, 0, 0, 255 }, image.Bgra);
    }

    [TestMethod]
    public void IconComposerReturnsNullForEmptyLayersAndPreservesPartialAlpha()
    {
        Assert.IsNull(IconComposer.Compose(Array.Empty<ThumbnailTexture>()));
        var image = IconComposer.Compose(new[] { new ThumbnailTexture(1, 1, [255, 0, 0, 128]) });
        Assert.IsNotNull(image);
        Assert.AreEqual((byte)128, image.Bgra[3]);
        Assert.AreEqual((byte)0, image.Bgra[0]);
        Assert.AreEqual((byte)0, image.Bgra[1]);
        Assert.AreEqual((byte)255, image.Bgra[2]);
    }

    [TestMethod]
    public void TypeSymbolsRemainDistinctAndChooseCompactDetailByRequestedSize()
    {
        ModuleResourceType[] types =
        [
            ModuleResourceType.Utc, ModuleResourceType.Uti, ModuleResourceType.Utp, ModuleResourceType.Utd,
            ModuleResourceType.Utm, ModuleResourceType.Utt, ModuleResourceType.Uts, ModuleResourceType.Utw
        ];
        var images = types.Select(type => TypeIconRenderer.Render(type, 64)).ToArray();
        Assert.AreEqual(types.Length, images.Select(image => Convert.ToHexString(image.Bgra)).Distinct().Count());
        Assert.AreEqual(TypeIconDetail.Compact, TypeIconRenderer.DetailFor(18));
        Assert.AreEqual(TypeIconDetail.Full, TypeIconRenderer.DetailFor(TypeIconRenderer.CompactSizeThreshold));
    }

    [TestMethod]
    public void UnsupportedTypeUsesGenericSymbol()
    {
        var image = TypeIconRenderer.Render(ModuleResourceType.Area, 64);
        Assert.AreEqual(64 * 64 * IconImage.BytesPerPixel, image.Bgra.Length);
        Assert.IsTrue(image.Bgra.Where((_, index) => index % 4 == 3).Any(alpha => alpha != 0));
    }
}