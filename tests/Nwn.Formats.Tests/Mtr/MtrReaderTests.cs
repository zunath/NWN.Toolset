using System.Globalization;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Formats.Mtr;

namespace Nwn.Formats.Tests.Mtr;

[TestClass]
public sealed class MtrReaderTests
{
    [TestMethod]
    public void BindingsParametersCommentsAndUnknownLinesPreserveTheirSource()
    {
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes("""
            // Material descriptors carry policy; the reader does not execute it.
            customshaderVS vertex_a
            CUSTOMSHADERfs fragment_b // mixed directive case
            customshaderGS NULL
            customshaderPSH fs_legacy_tinter
            customshaderVSH vs_legacy_body
            texture0 diffuse_a
            texture7 mask_a
            texture10 NULL
            renderhint NormalTangents
            parameter float exactRgb 16777216.0
            parameter float vectorValue 0.25 0.5 0.75 1.0
            parameter int signedValue -2147483648
            parameter float Case 1
            parameter float case 2
            parameter bool futureValue 1
            transparency 1
            """.Replace("\n", "\r\n"))).ToArray();
        var material = MtrReader.Read(bytes);
        Assert.AreEqual("vertex_a", material.Shaders[MtrShaderStage.Vertex]);
        Assert.AreEqual("fragment_b", material.Shaders[MtrShaderStage.Fragment]);
        Assert.IsNull(material.Shaders[MtrShaderStage.Geometry]);
        Assert.AreEqual("mask_a", material.Textures[7]);
        Assert.IsTrue(material.Textures.ContainsKey(10));
        Assert.IsNull(material.Textures[10]);
        Assert.IsFalse(material.Textures.ContainsKey(1));
        Assert.AreEqual("NormalTangents", material.RenderHint);
        Assert.AreEqual(16777216d, material.Parameters["exactRgb"].Values[0]);
        CollectionAssert.AreEqual(new double[] { 0.25, 0.5, 0.75, 1 }, material.Parameters["vectorValue"].Values.ToArray());
        Assert.AreEqual(MtrParameterKind.Int32, material.Parameters["signedValue"].Kind);
        Assert.AreEqual((double)int.MinValue, material.Parameters["signedValue"].Values[0]);
        Assert.AreEqual(1d, material.Parameters["Case"].Values[0]);
        Assert.AreEqual(2d, material.Parameters["case"].Values[0]);
        Assert.AreEqual("fs_legacy_tinter", material.RawShaderBindings["CUSTOMSHADERPSH"]);
        Assert.AreEqual("vs_legacy_body", material.RawShaderBindings["customshaderVSH"]);
        Assert.IsTrue(material.RawShaderBindings.Keys.Contains("customshaderPSH", StringComparer.Ordinal));
        Assert.AreEqual("bool", material.Parameters["futureValue"].TypeName);
        Assert.AreEqual(MtrParameterKind.Uninterpreted, material.Parameters["futureValue"].Kind);
        CollectionAssert.AreEqual(new[] { "1" }, material.Parameters["futureValue"].RawValues.ToArray());
        Assert.AreEqual(0, material.Parameters["futureValue"].Values.Count);
        CollectionAssert.AreEqual(new[] { "16777216.0" }, material.Parameters["exactRgb"].RawValues.ToArray());
        Assert.AreEqual(1, material.UnrecognizedDirectives.Count);
        CollectionAssert.AreEqual(bytes, material.CopySourceBytes());
        bytes[0] = 0;
        Assert.AreNotEqual(bytes[0], material.CopySourceBytes()[0]);
    }

    [TestMethod]
    public void NumericParsingUsesInvariantCultureAndKeepsFloatPrecision()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var material = MtrReader.Read("parameter float value 1.25e-2"u8);
            Assert.AreEqual((double)0.0125f, material.Parameters["value"].Values[0]);
            Assert.ThrowsExactly<FormatException>(() => MtrReader.Read("parameter float value 1,25"u8));
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [TestMethod]
    public void MalformedOrAmbiguousSupportedDirectivesFailWithTheirLineNumber()
    {
        string[] inputs =
        [
            "texture0 first\ntexture0 second",
            "customshaderVS first\ncustomshaderVS second",
            "customshaderPSH first\nCUSTOMSHADERpsh second",
            "renderhint first\nrenderhint second",
            "parameter int value 1\nparameter int value 2",
            "parameter bool value yes\nparameter int value 1",
            "// first line\nparameter int value 2147483648",
            "// first line\nparameter int value 1 2",
            "// first line\nparameter float value NaN",
            "// first line\nparameter float value Infinity",
            "// first line\nparameter float value 1e100",
            "// first line\nparameter float value 1 2 3 4 5",
        ];
        foreach (var input in inputs)
        {
            var exception = Assert.ThrowsExactly<FormatException>(() => MtrReader.Read(Encoding.UTF8.GetBytes(input)));
            StringAssert.Contains(exception.Message, "line 2:");
        }
    }

    [TestMethod]
    public void BoundsAndMalformedEncodingFailBeforeReturningAnUnboundedDescriptor()
    {
        Assert.ThrowsExactly<FormatException>(() => MtrReader.Read("texture0 sample"u8, new() { MaximumInputBytes = 4 }));
        Assert.ThrowsExactly<FormatException>(() => MtrReader.Read("texture0 sample"u8, new() { MaximumLineLength = 4 }));
        Assert.ThrowsExactly<FormatException>(() => MtrReader.Read("texture0 sample\ntexture1 normal"u8, new() { MaximumDirectiveCount = 1 }));
        Assert.ThrowsExactly<FormatException>(() => MtrReader.Read(new byte[] { 0xFF }));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => MtrReader.Read([], new() { MaximumDirectiveCount = 0 }));
        var unknown = MtrReader.Read("texture15 unsupported\nfuture statement values\ntexture2\nrenderhint\ntexture1 Image Texture.001"u8);
        Assert.AreEqual(5, unknown.UnrecognizedDirectives.Count);
        Assert.AreEqual(0, unknown.Textures.Count);
        Assert.IsNull(unknown.RenderHint);
    }
}
