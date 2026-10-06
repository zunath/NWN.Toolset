using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Formats.Tlk;

namespace Nwn.Formats.Tests.Tlk;

[TestClass]
public sealed class TlkRoundTripTests
{
    private static TlkTable SampleTable()
    {
        var table = new TlkTable(languageId: 0);
        table.Entries.Add(new TlkEntry("First entry"));
        table.Entries.Add(new TlkEntry(""));
        table.Entries.Add(new TlkEntry("Third entry with accents: éüñ"));
        table.Entries.Add(new TlkEntry("Voiced line", "xm_vo_0001", 2.5f));
        return table;
    }

    [TestMethod]
    public void WriteThenRead_RoundTrips()
    {
        var original = SampleTable();
        var bytes = TlkWriter.Write(original);
        var read = TlkReader.Read(bytes);

        Assert.AreEqual(original.LanguageId, read.LanguageId);
        Assert.AreEqual(original.Entries.Count, read.Entries.Count);
        for (var i = 0; i < original.Entries.Count; i++)
        {
            Assert.AreEqual(original.Entries[i], read.Entries[i]);
        }
    }

    [TestMethod]
    public void Write_IsDeterministic()
    {
        CollectionAssert.AreEqual(TlkWriter.Write(SampleTable()), TlkWriter.Write(SampleTable()));
    }

    [TestMethod]
    public void CustomStrRef_RoundTripsThroughIndex()
    {
        var strRef = TlkTable.ToCustomStrRef(42);
        Assert.AreEqual(0x01000000u + 42, strRef);
        Assert.AreEqual(42, TlkTable.FromCustomStrRef(strRef));
    }

    [TestMethod]
    public void Read_TruncatedHeader_Throws() => Assert.ThrowsExactly<FormatException>(() => TlkReader.Read(TlkWriter.Write(SampleTable())[..10]));

    [TestMethod]
    public void Read_WrongFileType_Throws()
    {
        var bytes = TlkWriter.Write(SampleTable());
        bytes[0] = (byte)'X';
        Assert.ThrowsExactly<FormatException>(() => TlkReader.Read(bytes));
    }

    [TestMethod]
    public void Read_EntryCountRequiresMoreBytesThanPresent_Throws()
    {
        var bytes = TlkWriter.Write(SampleTable());
        BitConverter.GetBytes(1000u).CopyTo(bytes, 12); // StringCount
        Assert.ThrowsExactly<FormatException>(() => TlkReader.Read(bytes));
    }

    [TestMethod]
    public void Read_StringOffsetPastEndOfFile_Throws()
    {
        var bytes = TlkWriter.Write(SampleTable());
        // First entry's OffsetToString is at header(20) + 0*40 + 16(flags+soundresref skipped:4+16=20) ... + 8 (volume+pitch) = offset 20+20+8=48.
        BitConverter.GetBytes(1_000_000u).CopyTo(bytes, 48);
        Assert.ThrowsExactly<FormatException>(() => TlkReader.Read(bytes));
    }
}
