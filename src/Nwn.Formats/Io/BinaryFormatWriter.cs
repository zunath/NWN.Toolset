using System.Buffers.Binary;
using System.Text;

namespace Nwn.Formats.Io;

/// <summary>Little-endian primitive writes into an in-memory buffer. Used for the small, structured
/// parts of every format (headers, tables, GFF/TLK bodies); ERF resource DATA is streamed straight
/// to the output stream instead of passing through here, so a multi-gigabyte hak never needs to fit
/// in memory at once.</summary>
public sealed class BinaryFormatWriter
{
    private readonly List<byte> _buffer = [];

    public int Position => _buffer.Count;

    public void WriteByte(byte value) => _buffer.Add(value);

    public void WriteUInt16(ushort value)
    {
        Span<byte> span = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(span, value);
        _buffer.AddRange(span.ToArray());
    }

    public void WriteInt16(short value)
    {
        Span<byte> span = stackalloc byte[2];
        BinaryPrimitives.WriteInt16LittleEndian(span, value);
        _buffer.AddRange(span.ToArray());
    }

    public void WriteUInt32(uint value)
    {
        Span<byte> span = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(span, value);
        _buffer.AddRange(span.ToArray());
    }

    public void WriteInt32(int value)
    {
        Span<byte> span = stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(span, value);
        _buffer.AddRange(span.ToArray());
    }

    public void WriteUInt64(ulong value)
    {
        Span<byte> span = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(span, value);
        _buffer.AddRange(span.ToArray());
    }

    public void WriteInt64(long value)
    {
        Span<byte> span = stackalloc byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(span, value);
        _buffer.AddRange(span.ToArray());
    }

    public void WriteSingle(float value)
    {
        Span<byte> span = stackalloc byte[4];
        BinaryPrimitives.WriteSingleLittleEndian(span, value);
        _buffer.AddRange(span.ToArray());
    }

    public void WriteDouble(double value)
    {
        Span<byte> span = stackalloc byte[8];
        BinaryPrimitives.WriteDoubleLittleEndian(span, value);
        _buffer.AddRange(span.ToArray());
    }

    public void WriteBytes(ReadOnlySpan<byte> bytes) => _buffer.AddRange(bytes.ToArray());

    /// <summary>Writes exactly <paramref name="width"/> bytes: <paramref name="value"/> as ASCII,
    /// NUL-padded (or truncated -- callers validate length up front so this never truncates real
    /// data).</summary>
    public void WriteFixedAscii(string value, int width)
    {
        var bytes = Encoding.ASCII.GetBytes(value);
        if (bytes.Length > width)
        {
            throw new ArgumentException($"'{value}' does not fit in {width} bytes.", nameof(value));
        }

        Span<byte> field = stackalloc byte[width];
        bytes.CopyTo(field);
        _buffer.AddRange(field.ToArray());
    }

    public void WriteAscii(string value) => _buffer.AddRange(Encoding.ASCII.GetBytes(value));

    /// <summary>Backfills a previously-written little-endian uint32 (used for offset/size fields
    /// whose value is only known once the rest of the structure has been laid out).</summary>
    public void PatchUInt32(int position, uint value)
    {
        Span<byte> span = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(span, value);
        for (var i = 0; i < 4; i++)
        {
            _buffer[position + i] = span[i];
        }
    }

    public byte[] ToArray() => [.. _buffer];
}
