using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Preview.Dds;

namespace Nwn.Preview.Tests.Dds;

[TestClass]
public sealed class NativeDdsCorpusTests
{
    [TestMethod]
    [DataRow("sw_ability/is_rocket.dds", 32, 32, 6, "e703d73bc6deac359ecfd42ced8ea91ecdb23ca35c1497006247b9b2657b254d")]
    [DataRow("sw_cr_creature/a_spiderbldbk.dds", 512, 512, 10, "88d3750415db027d20c7120b2a70a1bed3964507f41bee8834ab8ebad802d5c0")]
    public void SwlorCompactDdsSamples_DecodeAsBoundedBc1Surfaces(string relativePath, int width, int height, int mipCount, string pillowRgbaSha256)
    {
        var root = Environment.GetEnvironmentVariable("SWLOR_TEST_HAKS_ROOT");
        if (string.IsNullOrWhiteSpace(root))
            Assert.Inconclusive("Set SWLOR_TEST_HAKS_ROOT to run the read-only native DDS corpus check.");
        var path = Path.Combine(root!, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path))
            Assert.Inconclusive($"Native DDS sample is unavailable: {path}");

        var image = DdsDecoder.Decode(File.ReadAllBytes(path));
        Assert.AreEqual(width, image.Width);
        Assert.AreEqual(height, image.Height);
        Assert.AreEqual(mipCount, image.MipCount);
        Assert.AreEqual(DdsPixelFormat.Bc1, image.Format);
        Assert.AreEqual(width * height * 4, image.CopyRgbaBytes().Length);
        var digest = Convert.ToHexStringLower(SHA256.HashData(image.CopyRgbaBytes()));
        Assert.AreEqual(pillowRgbaSha256, digest, "RGBA pixels must match Pillow's independent DXT1 decode normalized from bottom-up rows.");
    }

    [TestMethod]
    [DataRow("XENOMECH_TEST_CONTENT_ROOT", "xm_tint/tm_932b2aa07fef2.dds", 2048, 2048, 12,
        "6a5a88b24074c4eb45e89b5ed580092abc7239f3ed8b8cc5feae58477edf4cd6",
        "34a976b0e4ea5650b24f2547cd790b758894aa9b2bb2cb50461508f060670bfd")]
    [DataRow("SWLOR_TEST_HAKS_ROOT", "sw_tint0/tm_e99bcc752e32b.dds", 512, 512, 1,
        "40f5b64a6c8b5e03fdf625cc4da881a1e30a34df84f2934ad1d545c7aea10e6e",
        "c3def9de95a0b41bc28765529d2299d03b387b4e0f98e73e33ee538a7a995541")]
    public void NativeUnsignedBc5ChannelsAndRowOrderMatchIndependentPythonDecode(string variable, string relativePath,
        int width, int height, int mipCount, string sourceHash, string expectedRgbaHash)
    {
        var root = Environment.GetEnvironmentVariable(variable);
        Assert.IsFalse(string.IsNullOrWhiteSpace(root), $"Select the read-only native DDS corpus with {variable}.");
        var path = Path.Combine(root!, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var bytes = File.ReadAllBytes(path);
        Assert.AreEqual(sourceHash, Convert.ToHexStringLower(SHA256.HashData(bytes)));
        var image = DdsDecoder.Decode(bytes, new DdsDecodeOptions { StoredRowOrder = DdsStoredRowOrder.BottomUp });
        Assert.AreEqual(DdsPixelFormat.Bc5, image.Format);
        Assert.AreEqual(width, image.Width);
        Assert.AreEqual(height, image.Height);
        Assert.AreEqual(mipCount, image.MipCount);
        Assert.AreEqual(expectedRgbaHash, Convert.ToHexStringLower(SHA256.HashData(image.CopyRgbaBytes())));
    }
}
