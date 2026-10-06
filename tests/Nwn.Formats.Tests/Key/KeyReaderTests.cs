using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Formats.Key;

using Nwn.Formats.Resources;

namespace Nwn.Formats.Tests.Key;

[TestClass]
public sealed class KeyReaderTests
{
    [TestMethod]
    public void Read_SingleBifSingleResource_ResolvesBifAndIndex()
    {
        var built = KeyBifFixture.Build([new Resource("xm_stock_a", 2017, "data/nwn_base.bif", [1, 2, 3])]);
        var key = KeyReader.Read(built.KeyBytes);

        Assert.AreEqual(1, key.Bifs.Count);
        Assert.AreEqual("data/nwn_base.bif", key.Bifs[0].Filename);
        Assert.AreEqual(1, key.Resources.Count);
        var resource = key.Resources[0];
        Assert.AreEqual("xm_stock_a", resource.ResRef);
        Assert.AreEqual(ResourceType.TwoDa, resource.Type);
        Assert.AreEqual(0, resource.BifIndex);
        Assert.AreEqual(0, resource.IndexInBif);
    }

    [TestMethod]
    public void Read_BackslashBifPath_IsNormalizedToForwardSlashes()
    {
        var built = KeyBifFixture.Build([new Resource("xm_stock_a", 2017, @"data\nwn_base.bif", [1])]);
        var key = KeyReader.Read(built.KeyBytes);
        Assert.AreEqual("data/nwn_base.bif", key.Bifs[0].Filename);
    }

    [TestMethod]
    public void Read_MultipleBifsAndResources_EncodesResIdCorrectly()
    {
        var built = KeyBifFixture.Build([
            new Resource("xm_a", 2017, "data/one.bif", [1]),
            new Resource("xm_b", 2017, "data/one.bif", [2]),
            new Resource("xm_c", 2027, "data/two.bif", [3]),
        ]);
        var key = KeyReader.Read(built.KeyBytes);

        Assert.AreEqual(2, key.Bifs.Count);
        var a = key.Resources.Single(r => r.ResRef == "xm_a");
        var b = key.Resources.Single(r => r.ResRef == "xm_b");
        var c = key.Resources.Single(r => r.ResRef == "xm_c");
        Assert.AreEqual(0, a.BifIndex); Assert.AreEqual(0, a.IndexInBif);
        Assert.AreEqual(0, b.BifIndex); Assert.AreEqual(1, b.IndexInBif);
        Assert.AreEqual(1, c.BifIndex); Assert.AreEqual(0, c.IndexInBif);
        Assert.AreEqual(ResourceType.Utc, c.Type);
    }

    [TestMethod]
    public void Read_UnrecognizedTypeCode_ResolvesToNullType()
    {
        var built = KeyBifFixture.Build([new Resource("xm_unknown", 9999, "data/one.bif", [1])]);
        var key = KeyReader.Read(built.KeyBytes);
        Assert.IsNull(key.Resources[0].Type);
        Assert.AreEqual((ushort)9999, key.Resources[0].RawTypeCode);
    }

    [TestMethod]
    public void Find_ReturnsMatchingResourceByResRefAndType()
    {
        var built = KeyBifFixture.Build([
            new Resource("placeables", 2017, "data/nwn_base.bif", [1]),
            new Resource("placeables", 2027, "data/nwn_base.bif", [2]), // same resref, different type
        ]);
        var key = KeyReader.Read(built.KeyBytes);
        var found = key.Find(Resref.Parse("placeables"), ResourceType.TwoDa);
        Assert.IsNotNull(found);
        Assert.AreEqual(ResourceType.TwoDa, found!.Type);
    }
}
