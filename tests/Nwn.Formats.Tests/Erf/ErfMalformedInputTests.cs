using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Formats.Erf;

using Nwn.Formats.Resources;

namespace Nwn.Formats.Tests.Erf;

[TestClass]
public sealed class ErfMalformedInputTests
{
    private static byte[] ValidArchiveBytes()
    {
        using var stream = new MemoryStream();
        ErfWriter.Write(
            stream, ErfContainerKind.Hak,
            [ErfResourceSource.FromBytes(Resref.Parse("xm_a"), ResourceType.Utc, [1, 2, 3])],
            ErfBuildDate.ContentEpoch);
        return stream.ToArray();
    }

    [TestMethod]
    public void Read_TruncatedHeader_Throws()
    {
        var bytes = ValidArchiveBytes()[..50];
        Assert.ThrowsExactly<FormatException>(() => ErfArchive.Read(new MemoryStream(bytes), ownsStream: false));
    }

    [TestMethod]
    public void Read_TruncatedBeforeResourceData_Throws()
    {
        var bytes = ValidArchiveBytes();
        var truncated = bytes[..(bytes.Length - 2)];
        Assert.ThrowsExactly<FormatException>(() => ErfArchive.Read(new MemoryStream(truncated), ownsStream: false));
    }

    [TestMethod]
    public void Read_UnsupportedVersion_Throws()
    {
        var bytes = ValidArchiveBytes();
        bytes[4] = (byte)'V'; bytes[5] = (byte)'9'; bytes[6] = (byte)'.'; bytes[7] = (byte)'9';
        Assert.ThrowsExactly<FormatException>(() => ErfArchive.Read(new MemoryStream(bytes), ownsStream: false));
    }

    [TestMethod]
    public void Read_UnknownFileTypeTag_Throws()
    {
        var bytes = ValidArchiveBytes();
        bytes[0] = (byte)'X'; bytes[1] = (byte)'X'; bytes[2] = (byte)'X'; bytes[3] = (byte)' ';
        Assert.ThrowsExactly<FormatException>(() => ErfArchive.Read(new MemoryStream(bytes), ownsStream: false));
    }

    [TestMethod]
    public void Read_KeyListOffsetPastEndOfFile_Throws()
    {
        var bytes = ValidArchiveBytes();
        BitConverter.GetBytes((uint)(bytes.Length + 10_000)).CopyTo(bytes, 24); // OffsetToKeyList
        Assert.ThrowsExactly<FormatException>(() => ErfArchive.Read(new MemoryStream(bytes), ownsStream: false));
    }

    [TestMethod]
    public void Read_ImplausiblyLargeEntryCount_Throws()
    {
        var bytes = ValidArchiveBytes();
        BitConverter.GetBytes(0x7FFFFFFFu).CopyTo(bytes, 16); // EntryCount
        Assert.ThrowsExactly<FormatException>(() => ErfArchive.Read(new MemoryStream(bytes), ownsStream: false));
    }

    [TestMethod]
    public void Open_MalformedArchive_DisposesOwnedFileStreamForImmediateDelete()
    {
        var bytes = ValidArchiveBytes();
        bytes[5] = (byte)'9';
        var path = Path.Combine(Path.GetTempPath(), $"nwn-erf-invalid-{Guid.NewGuid():N}.hak");
        File.WriteAllBytes(path, bytes);

        try
        {
            Assert.ThrowsExactly<FormatException>(() => ErfArchive.Open(path));

            File.Delete(path);
            Assert.IsFalse(File.Exists(path));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [TestMethod]
    public void Read_MalformedArchive_DisposesOnlyOwnedStream()
    {
        var bytes = ValidArchiveBytes();
        bytes[5] = (byte)'9';
        var owned = new MemoryStream(bytes);
        var borrowed = new MemoryStream(bytes);

        Assert.ThrowsExactly<FormatException>(() => ErfArchive.Read(owned));
        Assert.ThrowsExactly<FormatException>(() => ErfArchive.Read(borrowed, ownsStream: false));

        Assert.IsFalse(owned.CanRead);
        Assert.IsTrue(borrowed.CanRead);
        owned.Dispose();
        borrowed.Dispose();
    }

    [TestMethod]
    public void Read_SparseArchiveWithExcessiveEntryCount_RejectsBeforeIndexAllocation()
    {
        var bytes = ValidArchiveBytes();
        BitConverter.GetBytes(uint.MaxValue).CopyTo(bytes, 16); // EntryCount
        using var stream = new SparseHeaderStream(bytes[..160]);

        var error = Assert.ThrowsExactly<FormatException>(() => ErfArchive.Read(stream, ownsStream: false));

        StringAssert.Contains(error.Message, "entry count");
    }

    [TestMethod]
    [DataRow(0x1f)]
    [DataRow(0x7f)]
    [DataRow(0x80)]
    public void Read_ControlOrNonAsciiByteInKeyEntry_Throws(int invalidByte)
    {
        var bytes = ValidArchiveBytes();
        // Key list starts right after the header + empty localized-string block (offset 160);
        // Native keys are printable ASCII; decoding must not replace invalid bytes with '?'.
        bytes[160] = (byte)invalidByte;

        Assert.ThrowsExactly<FormatException>(() => ErfArchive.Read(new MemoryStream(bytes), ownsStream: false));
    }

    private sealed class SparseHeaderStream(byte[] header) : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => long.MaxValue;
        public override long Position { get; set; }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

        public override int Read(Span<byte> buffer)
        {
            if (Position >= header.Length)
                return 0;
            var count = (int)Math.Min(buffer.Length, header.Length - Position);
            header.AsSpan((int)Position, count).CopyTo(buffer);
            Position += count;
            return count;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            Position = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => checked(Position + offset),
                SeekOrigin.End => checked(Length + offset),
                _ => throw new ArgumentOutOfRangeException(nameof(origin))
            };
            return Position;
        }

        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
