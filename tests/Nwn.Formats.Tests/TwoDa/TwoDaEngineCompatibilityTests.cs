using System.Buffers.Binary;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Formats.TwoDa;

namespace Nwn.Formats.Tests.TwoDa;

[TestClass]
public sealed class TwoDaEngineCompatibilityTests
{
    [TestMethod]
    public void EngineCompatibleText_ReadsBomDefaultShortRowsAndFinalPhrases()
    {
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(
            "2DA V2.0\r\n\r\nDEFAULT: \"fallback value\"\r\nLABEL VALUE NOTE\r\n" +
            "10 chicken 7 two words\r\n20 **** ****\r\n")).ToArray();

        var table = TwoDaReader.Read(bytes, TwoDaReadOptions.EngineCompatible);

        CollectionAssert.AreEqual(new[] { "LABEL", "VALUE", "NOTE" }, table.Columns.ToArray());
        Assert.AreEqual("10", table.Rows[0].Label);
        Assert.AreEqual("two words", table.GetValue(0, "NOTE"));
        Assert.IsNull(table.GetValue(1, "VALUE"));
        Assert.IsNull(table.Rows[1].Values[2]);
        Assert.AreEqual("fallback value", table.DefaultValue);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => table.GetValue(50, "NOTE"));
        Assert.ThrowsExactly<ArgumentException>(() => table.GetValue(0, "MISSING"));
    }

    [TestMethod]
    public void EngineCompatibleText_DecodesWindows1252WithoutThrowingFirst()
    {
        var bytes = Encoding.ASCII.GetBytes("2DA V2.0\n\nVALUE\n0 caf")
            .Concat(new byte[] { 0xE9, (byte)'\n' })
            .ToArray();

        var table = TwoDaReader.Read(bytes, TwoDaReadOptions.EngineCompatible);

        Assert.AreEqual("café", table.GetValue(0, "VALUE"));
    }

    [TestMethod]
    public void EngineCompatibleBinary_ReadsRowMajorCellsAndLabels()
    {
        var table = TwoDaReader.Read(BuildBinary(), TwoDaReadOptions.EngineCompatible);

        CollectionAssert.AreEqual(new[] { "LABEL", "VALUE" }, table.Columns.ToArray());
        CollectionAssert.AreEqual(new[] { "rowA", "rowB" }, table.Rows.Select(row => row.Label).ToArray());
        Assert.AreEqual("alpha", table.GetValue(0, "LABEL"));
        Assert.AreEqual("7", table.GetValue(0, "VALUE"));
        Assert.IsNull(table.GetValue(1, "LABEL"));
        Assert.AreEqual("beta", table.GetValue(1, "VALUE"));
    }

    [TestMethod]
    public void EngineCompatibleBinary_RejectsOffsetsOutsideDeclaredStringData()
    {
        var bytes = BuildBinary();
        var dataSizeOffset = bytes.Length - Encoding.ASCII.GetByteCount("alpha\0" + "7\0" + "****\0" + "beta\0") - 2;
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(dataSizeOffset, sizeof(ushort)), 10);

        Assert.ThrowsExactly<FormatException>(
            () => TwoDaReader.Read(bytes, TwoDaReadOptions.EngineCompatible));
    }

    [TestMethod]
    public void StrictProfile_StillRejectsBomDefaultHeadersAndNoncanonicalRows()
    {
        var withBom = Encoding.UTF8.GetPreamble()
            .Concat(Encoding.UTF8.GetBytes("2DA V2.0\n\nVALUE\n0 x\n"))
            .ToArray();
        Assert.ThrowsExactly<FormatException>(() => TwoDaReader.Read(withBom, TwoDaReadOptions.Strict));
        Assert.ThrowsExactly<FormatException>(() => TwoDaReader.Read(
            "2DA V2.0\n\nDEFAULT: x\nVALUE\n0 a phrase\n", TwoDaReadOptions.EngineCompatible with
            {
                AllowDefaultHeader = false,
                TextRowPolicy = TwoDaTextRowPolicy.RequireExactColumnCount
            }));
        Assert.ThrowsExactly<FormatException>(() => TwoDaReader.Read(
            "2DA V2.0\n\nVALUE\n0 a phrase\n"));
    }

    [TestMethod]
    public void EngineCompatibleText_EnforcesConfiguredCellLimit()
    {
        var options = TwoDaReadOptions.EngineCompatible with { MaximumCells = 1 };

        Assert.ThrowsExactly<FormatException>(() => TwoDaReader.Read(
            "2DA V2.0\n\nA B\n0 x y\n", options));
    }

    [TestMethod]
    public void Reader_RejectsUnknownTextRowPolicy()
    {
        var options = TwoDaReadOptions.Strict with { TextRowPolicy = (TwoDaTextRowPolicy)999 };

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => TwoDaReader.Read("2DA V2.0\n\nVALUE\n0 x\n", options));
    }

    private static byte[] BuildBinary()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);
        writer.Write("2DA V2.b\n"u8);
        writer.Write(Encoding.ASCII.GetBytes("LABEL\tVALUE\t"));
        writer.Write((byte)0);
        writer.Write(2u);
        writer.Write(Encoding.ASCII.GetBytes("rowA\trowB\t"));
        writer.Write((ushort)0);
        writer.Write((ushort)6);
        writer.Write((ushort)8);
        writer.Write((ushort)13);
        var values = Encoding.ASCII.GetBytes("alpha\0" + "7\0" + "****\0" + "beta\0");
        writer.Write((ushort)values.Length);
        writer.Write(values);
        return stream.ToArray();
    }
}
