using System.Numerics;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Preview.Pixels;
using Nwn.Toolset.Avalonia.Areas;
using Nwn.Toolset.Avalonia.Tests.Support;

namespace Nwn.Toolset.Avalonia.Tests.Areas;

[TestClass]
public sealed class AreaViewportTintShaderTests
{
    private static string FragmentShader() => (string)typeof(AreaViewportControl)
        .GetField("FragmentShaderBody", BindingFlags.NonPublic | BindingFlags.Static)!
        .GetRawConstantValue()!;

    [TestMethod]
    public void CustomRgbIsShadedWithTheHostSelectedSwatchRow()
    {
        var shader = FragmentShader();

        StringAssert.Contains(shader, "vec2(128.5 / 256.0, paletteRow),");
        StringAssert.Contains(shader,
            "paletteColor.rgb + (custom.rgb - swatchMidpoint) * shadeScale,");
        StringAssert.Contains(shader, "dot(rampShade, luminanceWeights) /");
        Assert.IsFalse(shader.Contains("custom.rgb * shadeScale", StringComparison.Ordinal),
            "custom RGB must not be scaled by a single reference row");
        Assert.IsFalse(shader.Contains("vec4 referenceShade", StringComparison.Ordinal),
            "the layer's first row only shades custom colors whose swatch is black");
    }

    [TestMethod]
    public void BlackSwatchesBorrowTheLayersFirstRowShading()
    {
        var shader = FragmentShader();

        StringAssert.Contains(shader, "if (dot(swatchMidpoint, luminanceWeights) < 0.5 / 255.0)");
        StringAssert.Contains(shader, "vec2(paletteU, referenceRow),");
        StringAssert.Contains(shader, "vec2(128.5 / 256.0, referenceRow),");
    }

    [TestMethod]
    public void CustomRgbTakesSwatchCoverageUnlessTheHostKeepsLayerCoverage()
    {
        var shader = FragmentShader();
        var source = ToolsetSourceFiles.Read("Areas", "AreaViewportControl.cs");

        StringAssert.Contains(shader, "uniform bool tintCustomUsesLayerCoverage;");
        StringAssert.Contains(shader, "if (tintCustomUsesLayerCoverage)");
        StringAssert.Contains(shader, "paletteColor.a = layer > 1.5 && layer < 3.5 ? referenceAlpha : 1.0;");
        StringAssert.Contains(source,
            "SetUniformBool(\"tintCustomUsesLayerCoverage\", material.TintCustomUsesLayerCoverage);");
        StringAssert.Contains(source, "tint?.CustomColorsUseLayerCoverage == true);");
    }

    [TestMethod]
    public void TintSurfacesTakeSwatchCoverageByDefault()
    {
        var image = new RgbaImage(1, 1, new byte[4]);
        var layers = Enumerable.Repeat(new AreaViewportTintLayer(Vector4.Zero, 0.5f), 10).ToArray();

        var surface = new AreaViewportTintSurface(image, image, null, false, 0f, layers);
        var cloak = new AreaViewportTintSurface(image, image, null, false, 0f, layers)
        {
            CustomColorsUseLayerCoverage = true,
        };

        Assert.IsFalse(surface.CustomColorsUseLayerCoverage);
        Assert.IsTrue(cloak.CustomColorsUseLayerCoverage);
    }

    [TestMethod]
    public void FlatColorPassesClearTheTintMapState()
    {
        var source = ToolsetSourceFiles.Read("Areas", "AreaViewportControl.cs");
        var start = source.IndexOf("private void SetUniformBool(string name, bool value)", StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, "SetUniformBool was not found.");
        var body = source[start..source.IndexOf("private void SetUniformBoolCore", start, StringComparison.Ordinal)];

        StringAssert.Contains(body, "name == \"hasTexture\" && !value");
        StringAssert.Contains(body, "SetUniformBoolCore(\"hasTintMap\", false)");
        StringAssert.Contains(body, "SetUniformBoolCore(\"hasTintAlpha\", false)");
    }
}
