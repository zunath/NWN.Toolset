using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Resources;
using Nwn.Formats.Resources;
using Nwn.Preview.Tga;

namespace Nwn.Preview.Tests.Tga;

[TestClass]
public sealed class NativeTgaCorpusTests
{
    [TestMethod]
    public void NwnStockGrayTgaWithEightDescriptorBitsResolvesAndDecodesAsOpaqueIntensity()
    {
        var installRoot = Environment.GetEnvironmentVariable("XENOMECH_NWN_INSTALL_ROOT");
        if (string.IsNullOrWhiteSpace(installRoot))
            Assert.Inconclusive("Set XENOMECH_NWN_INSTALL_ROOT to run the read-only licensed NWN texture check.");

        var dataRoot = Path.Combine(installRoot!, "data");
        var keyPath = Path.Combine(dataRoot, "nwn_base.key");
        if (!File.Exists(keyPath) || !File.Exists(Path.Combine(dataRoot, "textures_02.bif")))
            Assert.Inconclusive("The selected licensed NWN installation does not contain the required KEY/BIF files.");

        using var layer = ResourceLayer.FromKeyBif("licensed-nwn", keyPath, installRoot!, ResourceDuplicatePolicy.LastWins);
        using var resources = new ResourceResolver([layer]);
        var identity = new ResourceIdentity("pmh0_bicepl007", ResourceType.Tga);
        var handle = resources.ResolveHandle(identity);
        Assert.IsNotNull(handle);
        Assert.AreEqual(Path.GetFullPath(Path.Combine(dataRoot, "textures_02.bif")),
            Path.GetFullPath(handle.Winner.SourcePath));
        Assert.AreEqual("data/textures_02.bif", handle.Winner.Entry.Replace('\\', '/'));

        var bytes = handle.ReadBytes(1024);
        Assert.AreEqual(592, bytes.Length);
        Assert.AreEqual("ebc2f26bb7bed11995f26bf138f09a59eea0b80d849287593e1ae41ed629b8de",
            Convert.ToHexStringLower(SHA256.HashData(bytes)));
        CollectionAssert.AreEqual(Convert.FromHexString("000003CC000000CC00000000100020000808"), bytes[..18]);

        var image = TgaDecoder.Decode(bytes);
        Assert.AreEqual(16, image.Width);
        Assert.AreEqual(32, image.Height);
        // Pillow independently decodes this stock image as a single-channel L image with opaque RGBA conversion.
        Assert.AreEqual("88f74886ad85fafbcef5a836c2aad6504ef771311d6b78ea9bbf8ebca1121092",
            Convert.ToHexStringLower(SHA256.HashData(image.CopyRgbaBytes())));
        Assert.IsTrue(image.CopyRgbaBytes().Where((_, index) => index % 4 == 3).All(alpha => alpha == 255));
    }

    [TestMethod]
    [DataRow("sw_ability/bite.tga", 32, 32, "0fcb0917484df8ffebd2c28f554b6bb9c720c0cb8f8368b79e805d9e23b647b0")]
    public void SwlorNativeTga_DecodesToIndependentPillowPixels(string relativePath, int width, int height, string pillowRgbaSha256)
    {
        var root = Environment.GetEnvironmentVariable("SWLOR_TEST_HAKS_ROOT");
        if (string.IsNullOrWhiteSpace(root))
            Assert.Inconclusive("Set SWLOR_TEST_HAKS_ROOT to run the read-only native texture check.");
        var path = Path.Combine(root!, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path))
            Assert.Inconclusive($"Native TGA sample is unavailable: {path}");

        var image = TgaDecoder.Decode(File.ReadAllBytes(path));
        Assert.AreEqual(width, image.Width);
        Assert.AreEqual(height, image.Height);
        Assert.AreEqual(pillowRgbaSha256, Convert.ToHexStringLower(SHA256.HashData(image.CopyRgbaBytes())));
    }
}
