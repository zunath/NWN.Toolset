using Nwn.Formats.Io;

using Nwn.Formats.Resources;

namespace Nwn.Formats.Erf;

/// <summary>
/// Writes an ERF V1.0 container (used for HAK/MOD/ERF alike -- only the 4-byte file-type tag
/// differs). Deterministic by construction: entries are always sorted the same way and the caller
/// always supplies the build date, so writing the same resources twice produces byte-identical
/// output. Resource bytes are streamed straight from each source to the output stream -- never
/// buffered whole -- so this is safe to call for a hak far larger than available memory.
/// </summary>
public static class ErfWriter
{
    private const int HeaderSize = 160;
    private const int KeyEntrySize = 24;
    private const int ResourceEntrySize = 8;
    public const uint NoDescriptionStrRef = 0xFFFFFFFF;

    public static void Write(
        Stream output,
        ErfContainerKind kind,
        IReadOnlyList<ErfResourceSource> resources,
        ErfBuildDate buildDate,
        uint descriptionStrRef = NoDescriptionStrRef,
        IReadOnlyList<(uint LanguageId, string Text)>? localizedDescription = null)
    {
        ArgumentNullException.ThrowIfNull(output);
        localizedDescription ??= [];

        var duplicate = resources
            .GroupBy(r => (Name: r.ResRef.Value, r.Type))
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException($"Duplicate resource '{duplicate.Key.Name}.{ResourceTypes.GetExtension(duplicate.Key.Type)}'.");
        }

        var sorted = resources
            .OrderBy(r => r.ResRef.Value, StringComparer.Ordinal)
            .ThenBy(r => (ushort)r.Type)
            .ToList();

        var header = new BinaryFormatWriter();
        header.WriteFixedAscii(kind.ToFileTypeTag(), 4);
        header.WriteFixedAscii("V1.0", 4);
        header.WriteUInt32((uint)localizedDescription.Count);

        var localizedStringBytes = BuildLocalizedStringBlock(localizedDescription);
        header.WriteUInt32((uint)localizedStringBytes.Length);
        header.WriteUInt32((uint)sorted.Count);

        var offsetToLocalizedString = HeaderSize;
        var offsetToKeyList = offsetToLocalizedString + localizedStringBytes.Length;
        var offsetToResourceList = offsetToKeyList + sorted.Count * KeyEntrySize;
        var dataStart = offsetToResourceList + sorted.Count * ResourceEntrySize;

        header.WriteUInt32((uint)offsetToLocalizedString);
        header.WriteUInt32((uint)offsetToKeyList);
        header.WriteUInt32((uint)offsetToResourceList);
        header.WriteUInt32(buildDate.YearsSince1900);
        header.WriteUInt32(buildDate.DayOfYearZeroBased);
        header.WriteUInt32(descriptionStrRef);
        header.WriteBytes(new byte[116]); // reserved, always zero

        header.WriteBytes(localizedStringBytes);

        var resourceOffsets = new uint[sorted.Count];
        var cursor = (long)dataStart;
        for (var i = 0; i < sorted.Count; i++)
        {
            resourceOffsets[i] = checked((uint)cursor);
            cursor += sorted[i].Length;
        }

        for (var i = 0; i < sorted.Count; i++)
        {
            header.WriteFixedAscii(sorted[i].ResRef.Value, 16);
            header.WriteUInt32((uint)i);
            header.WriteUInt16((ushort)sorted[i].Type);
            header.WriteUInt16(0); // unused
        }

        foreach (var offset in resourceOffsets.Zip(sorted, (offset, source) => (offset, source.Length)))
        {
            header.WriteUInt32(offset.offset);
            header.WriteUInt32(checked((uint)offset.Length));
        }

        var headerBytes = header.ToArray();
        if (headerBytes.Length != dataStart)
        {
            throw new InvalidOperationException($"Internal layout error: header block is {headerBytes.Length} bytes, expected {dataStart}.");
        }

        output.Write(headerBytes);

        var copyBuffer = new byte[81920];
        foreach (var source in sorted)
        {
            using var sourceStream = source.OpenRead();
            long copied = 0;
            int read;
            while ((read = sourceStream.Read(copyBuffer, 0, copyBuffer.Length)) > 0)
            {
                output.Write(copyBuffer, 0, read);
                copied += read;
            }
            if (copied != source.Length)
            {
                throw new InvalidOperationException(
                    $"Resource '{source.ResRef.Value}' declared length {source.Length} but its source stream produced {copied} bytes.");
            }
        }
    }

    private static byte[] BuildLocalizedStringBlock(IReadOnlyList<(uint LanguageId, string Text)> entries)
    {
        var writer = new BinaryFormatWriter();
        foreach (var (languageId, text) in entries)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(text);
            writer.WriteUInt32(languageId);
            writer.WriteUInt32((uint)bytes.Length);
            writer.WriteBytes(bytes);
        }
        return writer.ToArray();
    }
}
