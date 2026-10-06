using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Formats.Key;

namespace Nwn.Formats.Tests.Key;

[TestClass]
public sealed class BifReaderTests
{
    [TestMethod]
    public void Read_ReturnsOneEntryPerResource_AndBytesRoundTrip()
    {
        var built = KeyBifFixture.Build([
            new Resource("xm_a", 2017, "data/one.bif", [10, 20, 30]),
            new Resource("xm_b", 2027, "data/one.bif", [40, 50]),
        ]);
        var bifBytes = built.BifBytesByFilename["data/one.bif"];
        var bif = BifReader.Read(bifBytes);

        Assert.AreEqual(2, bif.Resources.Count);
        var first = bif.Find(0)!;
        var second = bif.Find(1)!;
        CollectionAssert.AreEqual(new byte[] { 10, 20, 30 }, BifReader.ReadResource(bifBytes, first));
        CollectionAssert.AreEqual(new byte[] { 40, 50 }, BifReader.ReadResource(bifBytes, second));
        Assert.AreEqual(2017u, first.RawTypeCode);
        Assert.AreEqual(2027u, second.RawTypeCode);
    }

    [TestMethod]
    public void Find_UnknownIndex_ReturnsNull()
    {
        var built = KeyBifFixture.Build([new Resource("xm_a", 2017, "data/one.bif", [1])]);
        var bif = BifReader.Read(built.BifBytesByFilename["data/one.bif"]);
        Assert.IsNull(bif.Find(42));
    }
}
