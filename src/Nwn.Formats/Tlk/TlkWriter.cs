using Nwn.Formats.Io;

namespace Nwn.Formats.Tlk;

/// <summary>Writes a TLK V3.0 talk table. Deterministic: entry order is exactly
/// <see cref="TlkTable.Entries"/>'s order (the caller controls strref assignment by list position),
/// and there is no build-date-like field here to vary between runs.</summary>
public static class TlkWriter
{
    private const int HeaderSize = 20;
    private const int EntrySize = 40;
    private const uint TextPresent = 0x1;
    private const uint SoundPresent = 0x2;
    private const uint SoundLengthPresent = 0x4;

    public static byte[] Write(TlkTable table)
    {
        var stringEntriesOffset = HeaderSize + table.Entries.Count * EntrySize;

        var w = new BinaryFormatWriter();
        w.WriteFixedAscii("TLK ", 4);
        w.WriteFixedAscii("V3.0", 4);
        w.WriteUInt32(table.LanguageId);
        w.WriteUInt32((uint)table.Entries.Count);
        w.WriteUInt32((uint)stringEntriesOffset);

        var textBlocks = table.Entries.Select(e => System.Text.Encoding.Latin1.GetBytes(e.Text)).ToList();
        var runningOffset = 0;
        var textOffsets = new int[textBlocks.Count];
        for (var i = 0; i < textBlocks.Count; i++)
        {
            textOffsets[i] = runningOffset;
            runningOffset += textBlocks[i].Length;
        }

        for (var i = 0; i < table.Entries.Count; i++)
        {
            var entry = table.Entries[i];
            uint flags = TextPresent;
            if (entry.SoundResRef.Length > 0)
            {
                flags |= SoundPresent;
            }

            if (entry.SoundLength != 0f)
            {
                flags |= SoundLengthPresent;
            }

            w.WriteUInt32(flags);
            w.WriteFixedAscii(entry.SoundResRef, 16);
            w.WriteUInt32(0); // volume variance
            w.WriteUInt32(0); // pitch variance
            w.WriteUInt32((uint)textOffsets[i]);
            w.WriteUInt32((uint)textBlocks[i].Length);
            w.WriteSingle(entry.SoundLength);
        }

        foreach (var block in textBlocks)
        {
            w.WriteBytes(block);
        }

        return w.ToArray();
    }
}
