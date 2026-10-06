using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Preview.Plt;

namespace Nwn.Preview.Tests.Plt;

[TestClass]
public sealed class NativePltCorpusTests
{
    [TestMethod]
    [DataRow("sw_plc_cep/idye_001.plt", 32, 32, "4e308fee4e3fca1ab2b9b41f1d7c2b24bd66a0129296a8321dabd0e56bc307ce")]
    [DataRow("sw_pt_cloak/cloak_017.plt", 512, 512, "2bab1c188021846845b78fbb303a8b68b687fd1e97e68ac4844a36b306ddcc4e")]
    public void SwlorNativePlt_DecodesTopDownPixels(string relativePath, int width, int height, string normalizedPixelSha256)
    {
        var root = Environment.GetEnvironmentVariable("SWLOR_TEST_HAKS_ROOT");
        if (string.IsNullOrWhiteSpace(root))
            Assert.Inconclusive("Set SWLOR_TEST_HAKS_ROOT to run the read-only native texture check.");
        var path = Path.Combine(root!, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path))
            Assert.Inconclusive($"Native PLT sample is unavailable: {path}");

        var image = PltDecoder.Decode(File.ReadAllBytes(path));
        Assert.AreEqual(width, image.Width);
        Assert.AreEqual(height, image.Height);
        Assert.AreEqual(normalizedPixelSha256, Convert.ToHexStringLower(SHA256.HashData(image.CopyPixelBytes())));
    }
}
