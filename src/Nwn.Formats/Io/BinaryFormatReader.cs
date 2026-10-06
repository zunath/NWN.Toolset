using System.Buffers.Binary;
using System.Text;

namespace Nwn.Formats.Io;

/// <summary>Little-endian primitive reads shared by ERF/GFF/2DA/TLK, with bounds checks on every
/// read (never trusts a count/offset from the file without checking it fits the stream) so a
/// truncated or hostile file fails with a clear <see cref="FormatException"/> instead of an
/// out-of-range crash or silent over-read.</summary>
public sealed class BinaryFormatReader
{
    private readonly byte[] _buffer;
    private readonly int _length;
    private int _position;

    public BinaryFormatReader(byte[] buffer)
    {
        _buffer = buffer;
        _length = buffer.Length;
    }

    public int Position => _position;
    public int Length => _length;
    public int Remaining => _length - _position;

    public void Seek(int absolutePosition)
    {
        if (absolutePosition < 0 || absolutePosition > _length)
        {
            throw new FormatException($"Seek target {absolutePosition} is outside the file (length {_length}).");
        }

        _position = absolutePosition;
    }

    private ReadOnlySpan<byte> Take(int count)
    {
        if (count < 0 || count > Remaining)
        {
            throw new FormatException($"Attempted to read {count} bytes at offset {_position}, but only {Remaining} remain.");
        }

        var span = _buffer.AsSpan(_position, count);
        _position += count;
        return span;
    }

    public byte ReadByte() => Take(1)[0];
    public ushort ReadUInt16() => BinaryPrimitives.ReadUInt16LittleEndian(Take(2));
    public short ReadInt16() => BinaryPrimitives.ReadInt16LittleEndian(Take(2));
    public uint ReadUInt32() => BinaryPrimitives.ReadUInt32LittleEndian(Take(4));
    public int ReadInt32() => BinaryPrimitives.ReadInt32LittleEndian(Take(4));
    public ulong ReadUInt64() => BinaryPrimitives.ReadUInt64LittleEndian(Take(8));
    public long ReadInt64() => BinaryPrimitives.ReadInt64LittleEndian(Take(8));
    public float ReadSingle() => BinaryPrimitives.ReadSingleLittleEndian(Take(4));
    public double ReadDouble() => BinaryPrimitives.ReadDoubleLittleEndian(Take(8));
    public byte[] ReadBytes(int count) => Take(count).ToArray();

    /// <summary>Reads a fixed-width ASCII field (ERF file-type/version tags, GFF struct labels,
    /// TLK sound resrefs): trailing NUL bytes are trimmed, embedded ones are not expected.</summary>
    public string ReadFixedAscii(int width)
    {
        var bytes = Take(width);
        var nul = bytes.IndexOf((byte)0);
        var textLength = nul < 0 ? width : nul;
        return Encoding.ASCII.GetString(bytes[..textLength]);
    }

    public string ReadAscii(int byteCount) => Encoding.ASCII.GetString(Take(byteCount));

    /// <summary>Reads at an absolute offset without disturbing the reader's own cursor -- GFF and
    /// ERF both address their data blocks by absolute file offsets from several places.</summary>
    public T ReadAt<T>(int absoluteOffset, Func<BinaryFormatReader, T> read)
    {
        var saved = _position;
        Seek(absoluteOffset);
        try { return read(this); }
        finally { _position = saved; }
    }
}
