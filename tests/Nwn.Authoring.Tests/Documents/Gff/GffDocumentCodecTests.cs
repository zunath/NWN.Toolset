using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Documents.Gff;
using Nwn.Formats.Gff;

namespace Nwn.Authoring.Tests.Documents.Gff;

[TestClass]
public sealed class GffDocumentCodecTests
{
    [TestMethod]
    public void BinaryCodec_RoundTripsMutableGffValues()
    {
        var codec = new GffDocumentCodec();
        var source = new GffDocument
        {
            FileType = "UTC ",
            Root = new GffStruct().Add(GffField.String("Name", "Unit"))
        };

        var bytes = codec.Encode(source);
        var decoded = codec.Decode(bytes);
        decoded.Root.Add(GffField.Int("Level", 7));
        var roundTrip = codec.Decode(codec.Encode(decoded));

        Assert.AreEqual("UTC ", roundTrip.FileType);
        Assert.AreEqual("Unit", roundTrip.Root.Find("Name")!.AsString());
        Assert.AreEqual(7, roundTrip.Root.Find("Level")!.AsInt());
    }
}
