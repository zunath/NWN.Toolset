using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Formats.Gff;

namespace Nwn.Formats.Tests.Gff;

[TestClass]
public sealed class GffMalformedInputTests
{
    private static byte[] ValidBytes()
    {
        var root = new GffStruct().Add(GffField.String("Name", "abc")).Add(GffField.Int("Value", 5));
        return GffWriter.Write(new GffDocument { FileType = "UTC ", Root = root });
    }

    [TestMethod]
    public void Read_TruncatedHeader_Throws() => Assert.ThrowsExactly<FormatException>(() => GffReader.Read(ValidBytes()[..40]));

    [TestMethod]
    public void Read_UnsupportedVersion_Throws()
    {
        var bytes = ValidBytes();
        bytes[4] = (byte)'V'; bytes[5] = (byte)'9'; bytes[6] = (byte)'.'; bytes[7] = (byte)'9';
        Assert.ThrowsExactly<FormatException>(() => GffReader.Read(bytes));
    }

    [TestMethod]
    public void Read_StructOffsetPastEndOfFile_Throws()
    {
        var bytes = ValidBytes();
        BitConverter.GetBytes((uint)(bytes.Length + 1000)).CopyTo(bytes, 8); // StructOffset
        Assert.ThrowsExactly<FormatException>(() => GffReader.Read(bytes));
    }

    [TestMethod]
    public void Read_ZeroStructs_Throws()
    {
        var bytes = ValidBytes();
        BitConverter.GetBytes(0u).CopyTo(bytes, 12); // StructCount
        Assert.ThrowsExactly<FormatException>(() => GffReader.Read(bytes));
    }

    [TestMethod]
    public void Read_FieldIndicesByteCountNotMultipleOfFour_Throws()
    {
        var bytes = ValidBytes();
        BitConverter.GetBytes(3u).CopyTo(bytes, 44); // FieldIndicesCount (bytes)
        Assert.ThrowsExactly<FormatException>(() => GffReader.Read(bytes));
    }

    [TestMethod]
    public void Read_LabelCountImplausiblyLarge_Throws()
    {
        var bytes = ValidBytes();
        BitConverter.GetBytes(0x0FFFFFFFu).CopyTo(bytes, 28); // LabelCount
        Assert.ThrowsExactly<FormatException>(() => GffReader.Read(bytes));
    }

    [TestMethod]
    public void Field_LabelLongerThan16Bytes_Throws() =>
        Assert.ThrowsExactly<FormatException>(() => GffField.Int("ThisLabelIsWayTooLongForGff", 1));

    [TestMethod]
    public void Write_ResRefValueTooLong_Throws() => Assert.ThrowsExactly<FormatException>(() => GffField.ResRef("R", "waytoolongforaresref"));

    [TestMethod]
    public void ResRefField_AllowsEmptyValue()
    {
        // A ResRef FIELD (e.g. an unset ScriptSpawn/Conversation field) legitimately holds "" on
        // pervasively many stock-shaped blueprints; only Nwn.Formats.Resref (an owned resource's
        // own name) requires non-empty.
        var field = GffField.ResRef("ScriptSpawn", "");
        Assert.AreEqual("", field.AsResRef());
        var document = new GffDocument { FileType = "UTC ", Root = new GffStruct().Add(field) };
        var roundTripped = GffReader.Read(GffWriter.Write(document));
        Assert.AreEqual("", roundTripped.Root.Find("ScriptSpawn")!.AsResRef());
    }
}
