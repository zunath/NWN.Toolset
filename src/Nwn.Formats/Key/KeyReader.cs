using Nwn.Formats.Io;

using Nwn.Formats.Resources;

namespace Nwn.Formats.Key;

/// <summary>
/// Reads a KEY V1 file (the BioWare Aurora engine's master resource index -- a publicly documented
/// binary layout, re-implemented here from scratch against that public description, not copied from
/// any reference repository). Only the header, file table and key table are parsed (at most a few
/// hundred KB even for the full base game); resource bytes always live in a BIF, never the KEY
/// itself, so this reader never touches them.
/// </summary>
public static class KeyReader
{
    private const int HeaderSize = 64;
    private const int FileTableEntrySize = 12;
    private const int KeyTableEntrySize = 22;

    /// <summary>The ResID encoding used throughout KEY/BIF: the high 12 bits select a BIF (an index
    /// into the KEY's own file table), the low 20 bits select a resource within that BIF's variable
    /// resource table.</summary>
    private const int ResourceIndexBits = 20;
    private const uint ResourceIndexMask = (1u << ResourceIndexBits) - 1;

    public static KeyFile Read(byte[] bytes)
    {
        if (bytes.Length < HeaderSize)
        {
            throw new FormatException($"KEY file is only {bytes.Length} bytes; the header alone needs {HeaderSize}.");
        }

        var header = new BinaryFormatReader(bytes);
        var fileType = header.ReadFixedAscii(4);
        var fileVersion = header.ReadFixedAscii(4);
        if (fileType != "KEY " || fileVersion != "V1  ")
        {
            throw new FormatException($"Unrecognized KEY signature '{fileType}{fileVersion}'; only 'KEY V1' is supported.");
        }

        var bifCount = (long)header.ReadUInt32();
        var keyCount = (long)header.ReadUInt32();
        var offsetToFileTable = (long)header.ReadUInt32();
        var offsetToKeyTable = (long)header.ReadUInt32();
        _ = header.ReadUInt32(); // build year, not needed to read entries
        _ = header.ReadUInt32(); // build day

        // Sanity bounds, not real format invariants -- catches a corrupt/truncated file with a clear
        // message instead of an overflow or an out-of-memory allocation attempt.
        if (bifCount is < 0 or > 100_000)
        {
            throw new FormatException($"Implausible BIF count {bifCount}.");
        }

        if (keyCount is < 0 or > 10_000_000)
        {
            throw new FormatException($"Implausible key count {keyCount}.");
        }

        var streamLength = (long)bytes.Length;
        CheckRange(offsetToFileTable, bifCount * FileTableEntrySize, streamLength, "file table");
        CheckRange(offsetToKeyTable, keyCount * KeyTableEntrySize, streamLength, "key table");

        var reader = new BinaryFormatReader(bytes);
        var bifs = new List<KeyBifEntry>((int)bifCount);
        for (var i = 0; i < bifCount; i++)
        {
            var entryOffset = (int)(offsetToFileTable + i * FileTableEntrySize);
            var fileSize = reader.ReadAt(entryOffset, r => r.ReadUInt32());
            var filenameOffset = reader.ReadAt(entryOffset + 4, r => r.ReadUInt32());
            var filenameSize = reader.ReadAt(entryOffset + 8, r => r.ReadUInt16());
            CheckRange(filenameOffset, filenameSize, streamLength, $"BIF file table entry {i} filename");
            var rawName = reader.ReadAt((int)filenameOffset, r => r.ReadFixedAscii(filenameSize));
            bifs.Add(new KeyBifEntry(rawName.Replace('\\', '/'), fileSize));
        }

        var resources = new List<KeyResourceEntry>((int)keyCount);
        for (var i = 0; i < keyCount; i++)
        {
            var entryOffset = (int)(offsetToKeyTable + i * KeyTableEntrySize);
            var resRef = reader.ReadAt(entryOffset, r => r.ReadFixedAscii(16)).ToLowerInvariant();
            var typeCode = reader.ReadAt(entryOffset + 16, r => r.ReadUInt16());
            var resId = reader.ReadAt(entryOffset + 18, r => r.ReadUInt32());

            var bifIndex = (int)(resId >> ResourceIndexBits);
            var indexInBif = (int)(resId & ResourceIndexMask);
            if (bifIndex < 0 || bifIndex >= bifs.Count)
            {
                throw new FormatException($"Key table entry {i} ('{resRef}'): BIF index {bifIndex} is out of range for {bifs.Count} BIF(s).");
            }

            var type = ResourceTypes.TryGetByCode(typeCode, out var known) ? known : (ResourceType?)null;
            resources.Add(new KeyResourceEntry(resRef, type, typeCode, bifIndex, indexInBif));
        }

        return new KeyFile { Bifs = bifs, Resources = resources };
    }

    private static void CheckRange(long offset, long size, long streamLength, string what)
    {
        if (offset < 0 || size < 0 || offset > streamLength || size > streamLength - offset)
        {
            throw new FormatException($"The {what} (offset {offset}, size {size}) does not fit within the file (length {streamLength}).");
        }
    }
}
