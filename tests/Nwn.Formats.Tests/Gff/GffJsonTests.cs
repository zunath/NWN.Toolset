using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Formats.Gff;

namespace Nwn.Formats.Tests.Gff;

[TestClass]
public sealed class GffJsonTests
{
    private static GffDocument BuildDocumentWithEveryFieldType()
    {
        var nested = new GffStruct(3).Add(GffField.Byte("Depth", 2));
        var listItem = new GffStruct(1).Add(GffField.String("Name", "Item"));

        var root = new GffStruct(0xFFFFFFFF)
            .Add(GffField.Byte("AByte", 200))
            .Add(GffField.Char("AChar", -5))
            .Add(GffField.Word("AWord", 60000))
            .Add(GffField.Short("AShort", -12345))
            .Add(GffField.Dword("ADword", 4_000_000_000))
            .Add(GffField.Int("AnInt", -123456))
            .Add(GffField.Dword64("ADword64", 18_000_000_000_000_000_000UL))
            .Add(GffField.Int64("AnInt64", -9_000_000_000_000_000_000L))
            .Add(GffField.Float("AFloat", 3.14159f))
            .Add(GffField.Double("ADouble", 2.718281828459045))
            .Add(GffField.String("AString", "Hello éü"))
            .Add(GffField.ResRef("AResRef", "xm_persist_npc"))
            .Add(GffField.Void("AVoid", [0, 1, 2, 250, 251, 255]))
            .Add(GffField.Struct("AStruct", nested))
            .Add(GffField.List("AList", [listItem]))
            .Add(GffField.List("AnEmptyList", []))
            .Add(GffField.LocString("ALocString", new GffLocString
            {
                Strings = [GffLocStringEntry.FromLanguageGender(0, 0, "English"), GffLocStringEntry.FromLanguageGender(1, 1, "French, female")],
            }))
            .Add(GffField.LocString("AStringRefOnly", GffLocString.FromStringRef(999)));

        return new GffDocument { FileType = "UTC ", Root = root };
    }

    [TestMethod]
    public void ToJsonFromJson_RoundTripsToAnEqualDocument()
    {
        var original = BuildDocumentWithEveryFieldType();
        var json = GffJson.ToJson(original);
        var roundTripped = GffJson.FromJson(json);

        var originalBytes = GffWriter.Write(original);
        var roundTrippedBytes = GffWriter.Write(roundTripped);
        CollectionAssert.AreEqual(originalBytes, roundTrippedBytes);
    }

    [TestMethod]
    public void FieldOrder_IsPreservedThroughJson()
    {
        var original = BuildDocumentWithEveryFieldType();
        var roundTripped = GffJson.FromJson(GffJson.ToJson(original));
        CollectionAssert.AreEqual(original.Root.Fields.Select(f => f.Label).ToList(), roundTripped.Root.Fields.Select(f => f.Label).ToList());
    }

    [TestMethod]
    public void BinaryToJsonToBinary_IsByteIdentical()
    {
        var original = BuildDocumentWithEveryFieldType();
        var bytes = GffWriter.Write(original);
        var fromBinary = GffReader.Read(bytes);
        var json = GffJson.ToJson(fromBinary);
        var fromJson = GffJson.FromJson(json);
        var rewritten = GffWriter.Write(fromJson);
        CollectionAssert.AreEqual(bytes, rewritten);
    }

    [TestMethod]
    public void FromJson_MissingRoot_Throws() => Assert.ThrowsExactly<FormatException>(() => GffJson.FromJson("""{"fileType":"UTC "}"""));

    [TestMethod]
    public void FromJson_UnknownFieldType_Throws() => Assert.ThrowsExactly<FormatException>(() =>
        GffJson.FromJson("""{"fileType":"UTC ","root":{"id":0,"fields":{"X":{"type":"bogus","value":1}}}}"""));
}
