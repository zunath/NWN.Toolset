using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Resources;
using Nwn.Preview.Icons;

namespace Nwn.Preview.Tests.Icons;

[TestClass]
public sealed class TypeIconRendererTests
{
    private const int Size = 64;
    private static readonly ModuleResourceType[] PaletteTypes = [ModuleResourceType.Utc,ModuleResourceType.Uti,ModuleResourceType.Utp,ModuleResourceType.Utd,
        ModuleResourceType.Utm,ModuleResourceType.Utt,ModuleResourceType.Uts,ModuleResourceType.Utw];
    public static IEnumerable<object[]> PaletteTypeCases() => PaletteTypes.Select(type => new object[] { type });
    private static int OpaquePixels(IconImage image)
    {
        var count = 0;
        for (var i = 3; i < image.Bgra.Length; i += IconImage.BytesPerPixel)
        {
            if (image.Bgra[i] > 0)
            {
                count++;
            }
        }
        return count;
    }

    [TestMethod]
    [DynamicData(nameof(PaletteTypeCases))]
    public void Every_Palette_Type_Draws_A_Symbol(ModuleResourceType type)
    {
        var image = TypeIconRenderer.Render(type, Size);
        Assert.AreEqual(Size, image.Width);
        Assert.AreEqual(Size, image.Height);
        Assert.IsTrue(OpaquePixels(image) > Size, $"{type} should cover a meaningful part of the tile");
    }

    [TestMethod]
    [DynamicData(nameof(PaletteTypeCases))]
    public void Symbols_Do_Not_Fill_The_Whole_Tile(ModuleResourceType type)
    {
        Assert.IsTrue(OpaquePixels(TypeIconRenderer.Render(type, Size)) < Size * Size, "the tile's own surface should show around the symbol");
    }

    [TestMethod]
    public void Every_Type_Gets_A_Distinguishable_Symbol()
    {
        var rendered = PaletteTypes.Select(type => Convert.ToHexString(TypeIconRenderer.Render(type, Size).Bgra)).ToList();
        Assert.AreEqual(PaletteTypes.Length, rendered.Distinct().Count());
    }

    [TestMethod]
    public void An_Unhandled_Type_Still_Gets_A_Plate_Rather_Than_Nothing()
    {
        Assert.IsTrue(OpaquePixels(TypeIconRenderer.Render(ModuleResourceType.Area, Size)) > Size);
    }

    [TestMethod]
    public void The_Requested_Size_Is_Honoured()
    {
        var image = TypeIconRenderer.Render(ModuleResourceType.Utw, 200);
        Assert.AreEqual(200, image.Width);
        Assert.AreEqual(200 * 200 * IconImage.BytesPerPixel, image.Bgra.Length);
    }

    [TestMethod]
    public void A_Non_Positive_Size_Is_Rejected() => Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => TypeIconRenderer.Render(ModuleResourceType.Utp, 0));

    [TestMethod]
    public void Shading_Keeps_Alpha_And_Scales_Colour() => Assert.AreEqual(0xFF404040u, TypeIconPalette.Shade(0xFF808080, 0.5f));

    [TestMethod]
    public void Shading_Clamps_Rather_Than_Wrapping_At_Full_Brightness() => Assert.AreEqual(0xFFFFFFFFu, TypeIconPalette.Shade(0xFFC0C0C0, 4f));
}