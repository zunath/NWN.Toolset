using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Formats.Io;

namespace Nwn.Formats.Tests.Io;

/// <summary>Every little-endian primitive write <see cref="BinaryFormatWriter"/> offers, checked
/// against its exact byte layout - most are already exercised indirectly through GFF/ERF/TLK writer
/// round trips, but several (Int16, UInt64, Int64, Double, plain ASCII, the byte-patch) have no
/// direct coverage at all.</summary>
[TestClass]
public sealed class BinaryFormatWriterTests
{
    [TestMethod]
    public void WriteByte_AppendsOneByte_AndAdvancesPosition()
    {
        var writer = new BinaryFormatWriter();

        writer.WriteByte(0x7F);

        Assert.AreEqual(1, writer.Position);
        CollectionAssert.AreEqual(new byte[] { 0x7F }, writer.ToArray());
    }

    [TestMethod]
    public void WriteUInt16_IsLittleEndian()
    {
        var writer = new BinaryFormatWriter();

        writer.WriteUInt16(0x1234);

        CollectionAssert.AreEqual(new byte[] { 0x34, 0x12 }, writer.ToArray());
    }

    [TestMethod]
    public void WriteInt16_IsLittleEndian_AndPreservesNegativeValues()
    {
        var writer = new BinaryFormatWriter();

        writer.WriteInt16(-1);

        CollectionAssert.AreEqual(new byte[] { 0xFF, 0xFF }, writer.ToArray());
    }

    [TestMethod]
    public void WriteUInt32_IsLittleEndian()
    {
        var writer = new BinaryFormatWriter();

        writer.WriteUInt32(0x12345678);

        CollectionAssert.AreEqual(new byte[] { 0x78, 0x56, 0x34, 0x12 }, writer.ToArray());
    }

    [TestMethod]
    public void WriteInt32_IsLittleEndian_AndPreservesNegativeValues()
    {
        var writer = new BinaryFormatWriter();

        writer.WriteInt32(-1);

        CollectionAssert.AreEqual(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, writer.ToArray());
    }

    [TestMethod]
    public void WriteUInt64_IsLittleEndian()
    {
        var writer = new BinaryFormatWriter();

        writer.WriteUInt64(0x0102030405060708);

        CollectionAssert.AreEqual(new byte[] { 0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01 }, writer.ToArray());
    }

    [TestMethod]
    public void WriteInt64_IsLittleEndian_AndPreservesNegativeValues()
    {
        var writer = new BinaryFormatWriter();

        writer.WriteInt64(-1);

        CollectionAssert.AreEqual(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, writer.ToArray());
    }

    [TestMethod]
    public void WriteSingle_RoundTripsThroughBinaryPrimitives()
    {
        var writer = new BinaryFormatWriter();

        writer.WriteSingle(1.5f);

        Assert.AreEqual(1.5f, System.Buffers.Binary.BinaryPrimitives.ReadSingleLittleEndian(writer.ToArray()));
    }

    [TestMethod]
    public void WriteDouble_RoundTripsThroughBinaryPrimitives()
    {
        var writer = new BinaryFormatWriter();

        writer.WriteDouble(1.5);

        Assert.AreEqual(1.5, System.Buffers.Binary.BinaryPrimitives.ReadDoubleLittleEndian(writer.ToArray()));
    }

    [TestMethod]
    public void WriteBytes_AppendsVerbatim()
    {
        var writer = new BinaryFormatWriter();

        writer.WriteBytes(new byte[] { 1, 2, 3 });

        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, writer.ToArray());
    }

    [TestMethod]
    public void WriteFixedAscii_PadsWithNulBytesToTheFullWidth()
    {
        var writer = new BinaryFormatWriter();

        writer.WriteFixedAscii("ab", 4);

        CollectionAssert.AreEqual(new byte[] { (byte)'a', (byte)'b', 0, 0 }, writer.ToArray());
    }

    [TestMethod]
    public void WriteFixedAscii_ExactWidth_WritesNoPadding()
    {
        var writer = new BinaryFormatWriter();

        writer.WriteFixedAscii("abcd", 4);

        CollectionAssert.AreEqual(new byte[] { (byte)'a', (byte)'b', (byte)'c', (byte)'d' }, writer.ToArray());
    }

    [TestMethod]
    public void WriteFixedAscii_TooLongForTheWidth_Throws()
    {
        var writer = new BinaryFormatWriter();

        Assert.ThrowsExactly<ArgumentException>(() => writer.WriteFixedAscii("abcde", 4));
    }

    [TestMethod]
    public void WriteAscii_WritesExactlyTheStringsOwnLength()
    {
        var writer = new BinaryFormatWriter();

        writer.WriteAscii("hi");

        CollectionAssert.AreEqual(new byte[] { (byte)'h', (byte)'i' }, writer.ToArray());
    }

    [TestMethod]
    public void PatchUInt32_OverwritesAnAlreadyWrittenValue_WithoutMovingLaterBytes()
    {
        var writer = new BinaryFormatWriter();
        writer.WriteUInt32(0); // placeholder
        var patchPosition = 0;
        writer.WriteByte(0xAB); // a later byte the patch must not disturb

        writer.PatchUInt32(patchPosition, 0x12345678);

        CollectionAssert.AreEqual(new byte[] { 0x78, 0x56, 0x34, 0x12, 0xAB }, writer.ToArray());
    }

    [TestMethod]
    public void Position_TracksTheNumberOfBytesWrittenSoFar()
    {
        var writer = new BinaryFormatWriter();

        writer.WriteUInt32(0);
        writer.WriteByte(1);

        Assert.AreEqual(5, writer.Position);
    }
}
