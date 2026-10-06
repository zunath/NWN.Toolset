using Nwn.Formats.Io;

using Nwn.Formats.Resources;

namespace Nwn.Formats.Erf;

/// <summary>
/// A parsed ERF/HAK/MOD container opened for reading. Parsing reads only the header, key list, and
/// resource list (a few hundred bytes to a few KB even for a hak with thousands of entries) -- never
/// the resource data itself, which stays on disk until a caller asks to extract one entry.
/// </summary>
public sealed class ErfArchive : IDisposable
{
    private const uint MaximumEntryCount = 1_000_000;
    private const long MaximumIndexBytes = 64L * 1024 * 1024;
    private readonly Stream _stream;
    private readonly bool _ownsStream;

    public ErfContainerKind Kind { get; }
    public uint DescriptionStrRef { get; }
    public IReadOnlyList<ErfEntry> Entries { get; }

    private ErfArchive(Stream stream, bool ownsStream, ErfContainerKind kind, uint descriptionStrRef, IReadOnlyList<ErfEntry> entries)
    {
        _stream = stream;
        _ownsStream = ownsStream;
        Kind = kind;
        DescriptionStrRef = descriptionStrRef;
        Entries = entries;
    }

    public static ErfArchive Open(string path) => Read(File.OpenRead(path), ownsStream: true);

    public static ErfArchive Read(Stream stream, bool ownsStream = true)
    {
        ArgumentNullException.ThrowIfNull(stream);
        try
        {
            return Parse(stream, ownsStream);
        }
        catch
        {
            if (ownsStream)
            {
                stream.Dispose();
            }

            throw;
        }
    }

    private static ErfArchive Parse(Stream stream, bool ownsStream)
    {
        if (!stream.CanSeek)
        {
            throw new ArgumentException("ERF reading requires a seekable stream.", nameof(stream));
        }

        if (stream.Length < 160)
        {
            throw new FormatException($"ERF file is only {stream.Length} bytes; the header alone needs 160.");
        }

        // The header, key list, and resource list are always small; only these are buffered.
        // Resource DATA is read later, directly from `stream`, by CopyResourceTo/ReadAllBytes.
        stream.Position = 0;
        var headerBytes = new byte[160];
        ReadExactly(stream, headerBytes);
        var header = new BinaryFormatReader(headerBytes);

        // ReadFixedAscii only trims a trailing NUL, and the file-type tag pads with a space
        // ("MOD ", "HAK ", "ERF "), so this is always exactly 4 characters.
        var fileType = header.ReadFixedAscii(4);
        var fileVersion = header.ReadFixedAscii(4);
        if (fileVersion != "V1.0")
        {
            throw new FormatException($"Unsupported ERF version '{fileVersion}'; only V1.0 is supported.");
        }

        var kind = ErfContainerKinds.Parse(fileType);

        var languageCount = header.ReadUInt32();
        var localizedStringSize = header.ReadUInt32();
        var entryCount = header.ReadUInt32();
        var offsetToLocalizedString = header.ReadUInt32();
        var offsetToKeyList = header.ReadUInt32();
        var offsetToResourceList = header.ReadUInt32();
        _ = header.ReadUInt32(); // build year, not needed to read entries
        _ = header.ReadUInt32(); // build day
        var descriptionStrRef = header.ReadUInt32();

        if (entryCount > MaximumEntryCount)
        {
            throw new FormatException($"ERF entry count {entryCount} exceeds the configured index limit of {MaximumEntryCount}.");
        }

        var keyIndexBytes = checked((long)entryCount * 24);
        var resourceIndexBytes = checked((long)entryCount * 8);
        var tupleIndexBytes = checked((long)entryCount * 8);
        var totalIndexBytes = checked(keyIndexBytes + resourceIndexBytes + tupleIndexBytes);
        if (totalIndexBytes > MaximumIndexBytes || keyIndexBytes > Array.MaxLength
            || resourceIndexBytes > Array.MaxLength || entryCount > int.MaxValue)
        {
            throw new FormatException($"ERF index requires {totalIndexBytes} bytes; the configured limit is {MaximumIndexBytes}.");
        }

        var streamLength = stream.Length;
        CheckRange(offsetToKeyList, keyIndexBytes, streamLength, "key list");
        CheckRange(offsetToResourceList, resourceIndexBytes, streamLength, "resource list");
        CheckRange(offsetToLocalizedString, localizedStringSize, streamLength, "localized string block");
        if (languageCount > entryCount + 1_000_000) // sanity bound, not a real invariant of the format
        {
            throw new FormatException($"Implausible localized string language count {languageCount}.");
        }

        var keyBytes = new byte[(int)keyIndexBytes];
        stream.Position = offsetToKeyList;
        ReadExactly(stream, keyBytes);
        var keyReader = new BinaryFormatReader(keyBytes);

        var resourceBytes = new byte[(int)resourceIndexBytes];
        stream.Position = offsetToResourceList;
        ReadExactly(stream, resourceBytes);
        var resourceReader = new BinaryFormatReader(resourceBytes);

        var resourceOffsetsAndSizes = new (uint Offset, uint Size)[(int)entryCount];
        for (var i = 0; i < entryCount; i++)
        {
            resourceOffsetsAndSizes[i] = (resourceReader.ReadUInt32(), resourceReader.ReadUInt32());
        }

        var entries = new List<ErfEntry>((int)entryCount);
        for (var i = 0; i < entryCount; i++)
        {
            var resRefBytes = keyReader.ReadBytes(Resref.MaxLength);
            var resId = keyReader.ReadUInt32();
            var typeCode = keyReader.ReadUInt16();
            _ = keyReader.ReadUInt16(); // unused

            if (!Resref.TryReadArchiveKey(resRefBytes, out var resRef, out var resrefError))
            {
                throw new FormatException($"Entry {i}: {resrefError}");
            }

            if (resId >= entryCount)
            {
                throw new FormatException($"Entry {i}: ResID {resId} is out of range for {entryCount} resources.");
            }

            var (offset, size) = resourceOffsetsAndSizes[resId];
            CheckRange(offset, size, streamLength, $"resource '{resRef.Value}'");
            entries.Add(new ErfEntry(resRef, (ResourceType)typeCode, offset, size));
        }

        return new ErfArchive(stream, ownsStream, kind, descriptionStrRef, entries);
    }

    /// <summary>Copies one resource's raw bytes to <paramref name="destination"/> in bounded chunks
    /// -- safe for a resource far larger than available memory.</summary>
    public void CopyResourceTo(ErfEntry entry, Stream destination)
    {
        _stream.Position = entry.Offset;
        var buffer = new byte[81920];
        var remaining = (long)entry.Size;
        while (remaining > 0)
        {
            var chunk = (int)Math.Min(buffer.Length, remaining);
            ReadExactly(_stream, buffer.AsSpan(0, chunk));
            destination.Write(buffer, 0, chunk);
            remaining -= chunk;
        }
    }

    /// <summary>Convenience for small resources (GFF/2DA blueprints). Prefer
    /// <see cref="CopyResourceTo"/> for large binary art.</summary>
    public byte[] ReadAllBytes(ErfEntry entry)
    {
        using var memory = new MemoryStream(checked((int)entry.Size));
        CopyResourceTo(entry, memory);
        return memory.ToArray();
    }

    /// <summary>Opens a read-only, forward-only view of one resource's bytes directly against the
    /// underlying archive stream (no copy). Only one such view may be read at a time -- it seeks the
    /// shared stream on open, so fully read and dispose it before opening another. Intended for
    /// "repack this archive's resources unchanged" callers (e.g. patching one GFF resource and
    /// streaming everything else straight through into a new ERF via
    /// <see cref="Nwn.Formats.Erf.ErfResourceSource"/>).</summary>
    public Stream OpenResourceStream(ErfEntry entry) => new BoundedArchiveStream(_stream, entry.Offset, entry.Size);

    private sealed class BoundedArchiveStream : Stream
    {
        private readonly Stream _inner;
        private readonly long _end;

        public BoundedArchiveStream(Stream inner, long offset, long size)
        {
            _inner = inner;
            _inner.Position = offset;
            _end = offset + size;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count)
        {
            var remaining = _end - _inner.Position;
            if (remaining <= 0)
            {
                return 0;
            }

            var toRead = (int)Math.Min(count, remaining);
            return _inner.Read(buffer, offset, toRead);
        }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    public ErfEntry? Find(Resref resRef, ResourceType type) =>
        Entries.FirstOrDefault(e => e.ResRef == resRef && e.Type == type);

    private static void CheckRange(long offset, long size, long streamLength, string what)
    {
        if (offset < 0 || size < 0 || offset > streamLength || size > streamLength - offset)
        {
            throw new FormatException($"The {what} entry (offset {offset}, size {size}) does not fit within the file (length {streamLength}).");
        }
    }

    private static void ReadExactly(Stream stream, byte[] buffer) => stream.ReadExactly(buffer, 0, buffer.Length);
    private static void ReadExactly(Stream stream, Span<byte> buffer) => stream.ReadExactly(buffer);

    public void Dispose()
    {
        if (_ownsStream)
        {
            _stream.Dispose();
        }
    }
}
