using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Preview.Pixels;
using Nwn.Toolset.Avalonia.Areas;

namespace Nwn.Toolset.Avalonia.Tests.Areas;

[TestClass]
public sealed class AreaViewportContractTests
{
    [TestMethod]
    public void MeshMetadataCopiesAndExposesPaletteIndicesReadOnly()
    {
        var colors = new Dictionary<int, int> { [2] = 17 };
        var metadata = new AreaViewportMeshMetadata(colors, usesItemTintOverrides: true);
        colors[2] = 4;

        Assert.AreEqual(17, metadata.LayerColorIndices[2]);
        var exposed = (IDictionary<int, int>)metadata.LayerColorIndices;
        Assert.ThrowsExactly<NotSupportedException>(() => exposed[2] = 5);
        Assert.IsTrue(metadata.UsesItemTintOverrides);
    }

    [TestMethod]
    public void TintSurfaceCopiesSelectionsAndRejectsUnnormalizedInputs()
    {
        var layers = Enumerable.Repeat(new AreaViewportTintLayer(Vector4.Zero, 0.5f), 10).ToArray();
        var surface = new AreaViewportTintSurface(
            new RgbaImage(1, 1, new byte[] { 0, 0, 0, 255 }),
            new RgbaImage(1, 1, new byte[] { 255, 255, 255, 255 }),
            null,
            alphaUsesRedChannel: false,
            alphaCutoff: 0f,
            layers);
        layers[0] = new AreaViewportTintLayer(new Vector4(1f), 1f);

        Assert.AreEqual(Vector4.Zero, surface.Layers[0].CustomColor);
        Assert.AreEqual(0.5f, surface.Layers[0].PaletteRow);
        var exposed = (IList<AreaViewportTintLayer>)surface.Layers;
        Assert.ThrowsExactly<NotSupportedException>(() => exposed[0] = new AreaViewportTintLayer(Vector4.One, 1f));
        Assert.ThrowsExactly<ArgumentException>(() => new AreaViewportTintSurface(
            surface.Map,
            surface.Palette,
            null,
            false,
            0f,
            Enumerable.Repeat(new AreaViewportTintLayer(new Vector4(float.NaN), 0.5f), 10)));
    }
}
