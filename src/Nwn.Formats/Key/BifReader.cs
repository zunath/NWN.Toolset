using System.Buffers.Binary;
using Nwn.Formats.Io;

namespace Nwn.Formats.Key;

/// <summary>
/// Reads a BIF V1 file (the BioWare Aurora engine's resource archive -- publicly documented binary
/// layout, re-implemented from scratch against that public description). Only the header and the
/// variable resource table are parsed up front; resource bytes are read on demand from a
/// seekable stream or an explicitly supplied byte array.
/// </summary>
public static class BifReader
{
    private const int HeaderSize = 20;
    private const int VariableEntrySize = 16;
    private const long MaximumIndexBytes = 64L * 1024 * 1024;
    internal const int ResourceIndexByteBudget = 64;

    public static BifFile Read(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        using var stream = new MemoryStream(bytes, writable: false);
        return Read(stream);
    }

    /// <summary>Reads bounded metadata without loading any resource payload.</summary>
    public static BifFile Read(Stream stream, long maximumIndexBytes = MaximumIndexBytes)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentOutOfRangeException.ThrowIfNegative(maximumIndexBytes);
        if (!stream.CanSeek)
            throw new ArgumentException("BIF reading requires a seekable stream.", nameof(stream));
        if (stream.Length < HeaderSize)
        {
            throw new FormatException($"BIF file is only {stream.Length} bytes; the header alone needs {HeaderSize}.");
        }

        stream.Position = 0;
        var headerBytes = new byte[HeaderSize];
        stream.ReadExactly(headerBytes);
        var header = new BinaryFormatReader(headerBytes);
        var fileType = header.ReadFixedAscii(4);
        var fileVersion = header.ReadFixedAscii(4);
        if (fileType != "BIFF" || fileVersion != "V1  ")
        {
            throw new FormatException($"Unrecognized BIF signature '{fileType}{fileVersion}'; only 'BIFF V1' is supported.");
        }

        var variableResourceCount = (long)header.ReadUInt32();
        _ = header.ReadUInt32(); // fixed resource count -- always 0 in every BIF ever shipped, unused
        var variableTableOffset = (long)header.ReadUInt32();

        if (variableResourceCount is < 0 or > 10_000_000)
        {
            throw new FormatException($"Implausible variable resource count {variableResourceCount}.");
        }

        var indexBudget = Math.Min(MaximumIndexBytes, maximumIndexBytes);
        if (variableResourceCount * ResourceIndexByteBudget > indexBudget)
            throw new FormatException($"BIF resource index exceeds the configured limit of {indexBudget} bytes.");
        var streamLength = stream.Length;
        CheckRange(variableTableOffset, variableResourceCount * VariableEntrySize, streamLength, "variable resource table");

        stream.Position = variableTableOffset;
        var resources = new List<BifResourceEntry>((int)variableResourceCount);
        Span<byte> entryBytes = stackalloc byte[VariableEntrySize];
        for (var i = 0; i < variableResourceCount; i++)
        {
            stream.ReadExactly(entryBytes);
            var id = BinaryPrimitives.ReadUInt32LittleEndian(entryBytes);
            var offset = BinaryPrimitives.ReadUInt32LittleEndian(entryBytes[4..]);
            var fileSize = BinaryPrimitives.ReadUInt32LittleEndian(entryBytes[8..]);
            var typeCode = BinaryPrimitives.ReadUInt32LittleEndian(entryBytes[12..]);
            CheckRange(offset, fileSize, streamLength, $"variable resource table entry {i} data");

            var index = (int)(id & 0xFFFFF);
            resources.Add(new BifResourceEntry(index, offset, fileSize, typeCode));
        }

        return new BifFile { Resources = resources };
    }

    public static byte[] ReadResource(byte[] bytes, BifResourceEntry entry)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        using var stream = new MemoryStream(bytes, writable: false);
        return ReadResource(stream, entry);
    }

    /// <summary>Checks the requested payload's range and allocation limit before reading it.</summary>
    public static byte[] ReadResource(Stream stream, BifResourceEntry entry, long maximumResourceBytes = long.MaxValue)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentOutOfRangeException.ThrowIfNegative(maximumResourceBytes);
        CheckRange(entry.Offset, entry.FileSize, stream.Length, "BIF resource");
        if (entry.FileSize > maximumResourceBytes || entry.FileSize > Array.MaxLength)
            throw new FormatException($"BIF resource is {entry.FileSize} bytes; configured limit is {maximumResourceBytes}.");
        stream.Position = entry.Offset;
        var bytes = new byte[checked((int)entry.FileSize)];
        stream.ReadExactly(bytes);
        return bytes;
    }

    private static void CheckRange(long offset, long size, long streamLength, string what)
    {
        if (offset < 0 || size < 0 || offset > streamLength || size > streamLength - offset)
        {
            throw new FormatException($"The {what} (offset {offset}, size {size}) does not fit within the file (length {streamLength}).");
        }
    }
}
