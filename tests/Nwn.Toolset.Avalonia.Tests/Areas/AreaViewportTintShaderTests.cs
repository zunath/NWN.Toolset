using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Toolset.Avalonia.Areas;
using Nwn.Toolset.Avalonia.Tests.Support;

namespace Nwn.Toolset.Avalonia.Tests.Areas;

[TestClass]
public sealed class AreaViewportTintShaderTests
{
    [TestMethod]
    public void CustomMetalRgbKeepsTheNeutralMetalRowsReflection()
    {
        var shader = (string)typeof(AreaViewportControl)
            .GetField("FragmentShaderBody", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetRawConstantValue()!;

        StringAssert.Contains(shader, "vec4 referenceShade = textureLod(");
        StringAssert.Contains(shader, "paletteColor.a = layer > 1.5 && layer < 3.5 ? referenceShade.a : 1.0;");
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
