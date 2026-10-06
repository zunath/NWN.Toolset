using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Formats.Gff;

namespace Nwn.Formats.Tests.Gff;

[TestClass]
public sealed class GffRoundTripTests
{
    private static GffDocument BuildSampleDocument()
    {
        var child1 = new GffStruct(1)
            .Add(GffField.String("Name", "Child One"))
            .Add(GffField.Int("Value", -7));
        var child2 = new GffStruct(2)
            .Add(GffField.String("Name", "Child Two"))
            .Add(GffField.Int("Value", 42));

        var nested = new GffStruct(3).Add(GffField.Byte("Depth", 2));

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
            .Add(GffField.String("AString", "Hello, Xenomech! éü"))
            .Add(GffField.ResRef("AResRef", "xm_persist_npc"))
            .Add(GffField.Void("AVoid", [1, 2, 3, 4, 250, 251]))
            .Add(GffField.Struct("AStruct", nested))
            .Add(GffField.List("AList", [child1, child2]))
            .Add(GffField.LocString("ALocString", new GffLocString
            {
                StringRef = GffLocString.NoStringRef,
                Strings = [GffLocStringEntry.FromLanguageGender(0, 0, "English text"), GffLocStringEntry.FromLanguageGender(1, 0, "Texte francais")],
            }))
            .Add(GffField.LocString("AStringRefOnly", GffLocString.FromStringRef(12345)))
            .Add(GffField.List("AnEmptyList", []));

        return new GffDocument { FileType = "UTC ", Root = root };
    }

    [TestMethod]
    public void WriteThenRead_ProducesAnEqualDocument()
    {
        var original = BuildSampleDocument();
        var bytes = GffWriter.Write(original);
        var roundTripped = GffReader.Read(bytes);

        Assert.AreEqual(original.FileType, roundTripped.FileType);
        Assert.AreEqual(original.FileVersion, roundTripped.FileVersion);
        AssertStructsEqual(original.Root, roundTripped.Root);
    }

    [TestMethod]
    public void WriteReadWrite_IsByteIdenticalOnTheSecondWrite()
    {
        var original = BuildSampleDocument();
        var firstBytes = GffWriter.Write(original);
        var reRead = GffReader.Read(firstBytes);
        var secondBytes = GffWriter.Write(reRead);
        CollectionAssert.AreEqual(firstBytes, secondBytes);
    }

    [TestMethod]
    public void Write_IsDeterministic()
    {
        var a = GffWriter.Write(BuildSampleDocument());
        var b = GffWriter.Write(BuildSampleDocument());
        CollectionAssert.AreEqual(a, b);
    }

    [TestMethod]
    public void EmptyRootStruct_RoundTrips()
    {
        var document = new GffDocument { FileType = "IFO ", Root = new GffStruct(0xFFFFFFFF) };
        var bytes = GffWriter.Write(document);
        var read = GffReader.Read(bytes);
        Assert.AreEqual(0, read.Root.Fields.Count);
    }

    [TestMethod]
    public void SingleFieldStruct_UsesTheDirectFieldIndexEncoding()
    {
        // A struct with exactly one field stores the field index directly rather than via the
        // field-indices side table -- exercise that path explicitly.
        var document = new GffDocument { FileType = "UTI ", Root = new GffStruct().Add(GffField.Int("Only", 7)) };
        var bytes = GffWriter.Write(document);
        var read = GffReader.Read(bytes);
        Assert.AreEqual(1, read.Root.Fields.Count);
        Assert.AreEqual(7, read.Root.Find("Only")!.AsInt());
    }

    [TestMethod]
    public void DeeplyNestedLists_RoundTrip()
    {
        GffStruct MakeLevel(int depth)
        {
            var s = new GffStruct((uint)depth).Add(GffField.Int("Depth", depth));
            if (depth > 0)
            {
                s.Add(GffField.List("Inner", [MakeLevel(depth - 1)]));
            }

            return s;
        }

        var document = new GffDocument { FileType = "ARE ", Root = MakeLevel(10) };
        var bytes = GffWriter.Write(document);
        var read = GffReader.Read(bytes);
        AssertStructsEqual(document.Root, read.Root);
    }

    private static void AssertStructsEqual(GffStruct expected, GffStruct actual)
    {
        Assert.AreEqual(expected.StructId, actual.StructId);
        Assert.AreEqual(expected.Fields.Count, actual.Fields.Count);
        for (var i = 0; i < expected.Fields.Count; i++)
        {
            AssertFieldsEqual(expected.Fields[i], actual.Fields[i]);
        }
    }

    private static void AssertFieldsEqual(GffField expected, GffField actual)
    {
        Assert.AreEqual(expected.Label, actual.Label);
        Assert.AreEqual(expected.Type, actual.Type);
        switch (expected.Type)
        {
            case GffFieldType.Void:
                CollectionAssert.AreEqual(expected.AsVoid(), actual.AsVoid());
                break;
            case GffFieldType.Struct:
                AssertStructsEqual(expected.AsStruct(), actual.AsStruct());
                break;
            case GffFieldType.List:
                var expectedList = expected.AsList();
                var actualList = actual.AsList();
                Assert.AreEqual(expectedList.Count, actualList.Count);
                for (var i = 0; i < expectedList.Count; i++)
                {
                    AssertStructsEqual(expectedList[i], actualList[i]);
                }

                break;
            case GffFieldType.LocString:
                var expectedLoc = expected.AsLocString();
                var actualLoc = actual.AsLocString();
                Assert.AreEqual(expectedLoc.StringRef, actualLoc.StringRef);
                CollectionAssert.AreEqual(expectedLoc.Strings.ToList(), actualLoc.Strings.ToList());
                break;
            default:
                Assert.AreEqual(expected.Value, actual.Value);
                break;
        }
    }
}
