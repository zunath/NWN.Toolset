using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Formats.TwoDa;

namespace Nwn.Formats.Tests.TwoDa;

[TestClass]
public sealed class TwoDaReaderWriterTests
{
    private const string SampleText =
        "2DA V2.0\n\n" +
        "         Label       Cost\n" +
        "0        SHORT_SWORD 10\n" +
        "1        \"Long Bow\"  ****\n";

    [TestMethod]
    public void Read_ParsesColumnsRowsAndEmptyCells()
    {
        var table = TwoDaReader.Read(SampleText);
        CollectionAssert.AreEqual(new[] { "Label", "Cost" }, table.Columns.ToList());
        Assert.AreEqual(2, table.Rows.Count);
        Assert.AreEqual("SHORT_SWORD", table.GetValue(0, "Label"));
        Assert.AreEqual("10", table.GetValue(0, "Cost"));
        Assert.AreEqual("Long Bow", table.GetValue(1, "Label"));
        Assert.IsNull(table.GetValue(1, "Cost"));
    }

    [TestMethod]
    public void WriteThenRead_RoundTrips()
    {
        var original = TwoDaReader.Read(SampleText);
        var written = TwoDaWriter.Write(original);
        var reRead = TwoDaReader.Read(written);

        CollectionAssert.AreEqual(original.Columns.ToList(), reRead.Columns.ToList());
        Assert.AreEqual(original.Rows.Count, reRead.Rows.Count);
        for (var i = 0; i < original.Rows.Count; i++)
        {
            Assert.AreEqual(original.Rows[i].Label, reRead.Rows[i].Label);
            CollectionAssert.AreEqual(original.Rows[i].Values.ToList(), reRead.Rows[i].Values.ToList());
        }
    }

    [TestMethod]
    public void ValueWithSpaces_RoundTripsQuoted()
    {
        var table = new Nwn.Formats.TwoDa.TwoDaTable(["Label", "Description"]);
        table.AddRow("0", new Dictionary<string, string?> { ["Label"] = "x", ["Description"] = "has spaces" });
        var written = TwoDaWriter.Write(table);
        var read = TwoDaReader.Read(written);
        Assert.AreEqual("has spaces", read.GetValue(0, "Description"));
    }

    [TestMethod]
    public void Read_MissingHeader_Throws() => Assert.ThrowsExactly<FormatException>(() => TwoDaReader.Read("not a 2da file\n"));

    [TestMethod]
    public void Read_RowWithWrongColumnCount_Throws() => Assert.ThrowsExactly<FormatException>(() => TwoDaReader.Read("2DA V2.0\n\nLabel Cost\n0 SHORT_SWORD\n"));

    [TestMethod]
    public void Read_DuplicateRowLabel_Throws() => Assert.ThrowsExactly<FormatException>(() => TwoDaReader.Read("2DA V2.0\n\nLabel\n0 a\n0 b\n"));

    [TestMethod]
    public void Table_RejectsDuplicateColumnNames() => Assert.ThrowsExactly<ArgumentException>(() => _ = new Nwn.Formats.TwoDa.TwoDaTable(["Label", "label"]));
}
