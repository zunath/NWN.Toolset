using Nwn.Formats.Io;

namespace Nwn.Formats.Tlk;

/// <summary>Parses a TLK V3.0 talk table. Bounds-checks every string's offset/length against the
/// file before reading it.</summary>
public static class TlkReader
{
    private const int HeaderSize = 20;
    private const int EntrySize = 40;
    private const uint TextPresent = 0x1;
    private const uint SoundPresent = 0x2;
    private const uint SoundLengthPresent = 0x4;

    public static TlkTable Read(byte[] bytes)
    {
        if (bytes.Length < HeaderSize)
        {
            throw new FormatException($"TLK file is only {bytes.Length} bytes; the header alone needs {HeaderSize}.");
        }

        var r = new BinaryFormatReader(bytes);

        var fileType = r.ReadFixedAscii(4);
        var fileVersion = r.ReadFixedAscii(4);
        if (fileType != "TLK ")
        {
            throw new FormatException($"Expected TLK file type tag, found '{fileType}'.");
        }

        if (fileVersion != "V3.0")
        {
            throw new FormatException($"Unsupported TLK version '{fileVersion}'; only V3.0 is supported.");
        }

        var languageId = r.ReadUInt32();
        var stringCount = r.ReadUInt32();
        var stringEntriesOffset = r.ReadUInt32();

        var entryTableBytes = (long)stringCount * EntrySize;
        if (HeaderSize + entryTableBytes > bytes.Length)
        {
            throw new FormatException($"TLK declares {stringCount} entries, which needs {entryTableBytes} bytes past the header, but the file is only {bytes.Length} bytes.");
        }

        if (stringEntriesOffset > bytes.Length)
        {
            throw new FormatException($"TLK string data offset {stringEntriesOffset} is past the end of the file (length {bytes.Length}).");
        }

        var table = new TlkTable(languageId);
        for (var i = 0; i < stringCount; i++)
        {
            var flags = r.ReadUInt32();
            var soundResRef = r.ReadFixedAscii(16);
            _ = r.ReadUInt32(); // volume variance: unused by the engine
            _ = r.ReadUInt32(); // pitch variance: unused by the engine
            var offsetToString = r.ReadUInt32();
            var stringSize = r.ReadUInt32();
            var soundLength = r.ReadSingle();

            var text = "";
            if ((flags & TextPresent) != 0)
            {
                var absoluteOffset = (long)stringEntriesOffset + offsetToString;
                if (absoluteOffset < 0 || absoluteOffset > bytes.Length || stringSize > bytes.Length - absoluteOffset)
                {
                    throw new FormatException($"Entry {i}: string data (offset {absoluteOffset}, size {stringSize}) does not fit within the file.");
                }

                text = System.Text.Encoding.Latin1.GetString(bytes, (int)absoluteOffset, (int)stringSize);
            }

            table.Entries.Add(new TlkEntry(
                text,
                (flags & SoundPresent) != 0 ? soundResRef : "",
                (flags & SoundLengthPresent) != 0 ? soundLength : 0f));
        }

        return table;
    }
}
