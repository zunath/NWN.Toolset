using Nwn.Formats.Io;

namespace Nwn.Formats.Gff;

/// <summary>
/// Parses a GFF V3.2 byte blob into a <see cref="GffDocument"/> tree. Every offset/count taken from
/// the file is bounds-checked before use (truncated files, corrupt counts, and offsets past the end
/// of the buffer all fail with a <see cref="FormatException"/> naming the problem, never an
/// out-of-range crash).
/// </summary>
public static class GffReader
{
    private const int HeaderSize = 56;
    private const int StructEntrySize = 12;
    private const int FieldEntrySize = 12;
    private const int LabelSize = 16;

    public static GffDocument Read(byte[] bytes)
    {
        if (bytes.Length < HeaderSize)
        {
            throw new FormatException($"GFF file is only {bytes.Length} bytes; the header alone needs {HeaderSize}.");
        }

        var header = new BinaryFormatReader(bytes);

        var fileType = header.ReadFixedAscii(4);
        var fileVersion = header.ReadFixedAscii(4);
        if (fileVersion != "V3.2")
        {
            throw new FormatException($"Unsupported GFF version '{fileVersion}'; only V3.2 is supported.");
        }

        var structOffset = header.ReadUInt32();
        var structCount = header.ReadUInt32();
        var fieldOffset = header.ReadUInt32();
        var fieldCount = header.ReadUInt32();
        var labelOffset = header.ReadUInt32();
        var labelCount = header.ReadUInt32();
        var fieldDataOffset = header.ReadUInt32();
        var fieldDataByteCount = header.ReadUInt32();
        var fieldIndicesOffset = header.ReadUInt32();
        var fieldIndicesByteCount = header.ReadUInt32();
        var listIndicesOffset = header.ReadUInt32();
        var listIndicesByteCount = header.ReadUInt32();

        CheckBlock(bytes.Length, structOffset, (long)structCount * StructEntrySize, "struct array");
        CheckBlock(bytes.Length, fieldOffset, (long)fieldCount * FieldEntrySize, "field array");
        CheckBlock(bytes.Length, labelOffset, (long)labelCount * LabelSize, "label array");
        CheckBlock(bytes.Length, fieldDataOffset, fieldDataByteCount, "field data block");
        CheckBlock(bytes.Length, fieldIndicesOffset, fieldIndicesByteCount, "field indices block");
        CheckBlock(bytes.Length, listIndicesOffset, listIndicesByteCount, "list indices block");
        if (fieldIndicesByteCount % 4 != 0)
        {
            throw new FormatException("Field indices block size is not a multiple of 4.");
        }

        var labels = new string[labelCount];
        for (var i = 0; i < labelCount; i++)
        {
            labels[i] = header.ReadAt((int)(labelOffset + i * LabelSize), r => r.ReadFixedAscii(LabelSize));
        }

        var structEntries = new (uint Type, uint DataOrOffset, uint FieldCount)[structCount];
        for (var i = 0; i < structCount; i++)
        {
            header.Seek((int)(structOffset + i * StructEntrySize));
            structEntries[i] = (header.ReadUInt32(), header.ReadUInt32(), header.ReadUInt32());
        }

        var fieldEntries = new (uint Type, uint LabelIndex, uint DataOrOffset)[fieldCount];
        for (var i = 0; i < fieldCount; i++)
        {
            header.Seek((int)(fieldOffset + i * FieldEntrySize));
            fieldEntries[i] = (header.ReadUInt32(), header.ReadUInt32(), header.ReadUInt32());
        }

        var fieldIndices = new uint[fieldIndicesByteCount / 4];
        for (var i = 0; i < fieldIndices.Length; i++)
        {
            fieldIndices[i] = header.ReadAt((int)(fieldIndicesOffset + i * 4), r => r.ReadUInt32());
        }

        var context = new ReadContext(header, labels, structEntries, fieldEntries, fieldIndices,
            fieldDataOffset, fieldDataByteCount, listIndicesOffset, listIndicesByteCount);

        if (structCount == 0)
        {
            throw new FormatException("GFF file has no structs; a document always has a root struct.");
        }

        var root = context.ReadStruct(0);
        return new GffDocument { FileType = fileType, FileVersion = fileVersion, Root = root };
    }

    private static void CheckBlock(long fileLength, long offset, long size, string what)
    {
        if (size == 0)
        {
            return; // an empty block's offset is not meaningful in practice
        }

        if (offset < 0 || size < 0 || offset > fileLength || size > fileLength - offset)
        {
            throw new FormatException($"The {what} (offset {offset}, size {size}) does not fit within the file (length {fileLength}).");
        }
    }

    private sealed class ReadContext(
        BinaryFormatReader reader,
        string[] labels,
        (uint Type, uint DataOrOffset, uint FieldCount)[] structEntries,
        (uint Type, uint LabelIndex, uint DataOrOffset)[] fieldEntries,
        uint[] fieldIndices,
        uint fieldDataOffset,
        uint fieldDataByteCount,
        uint listIndicesOffset,
        uint listIndicesByteCount)
    {
        private int _structDepth;
        private const int MaxStructDepth = 64; // guards against a cyclic/hostile file; real content nests far shallower

        public GffStruct ReadStruct(uint structIndex)
        {
            if (structIndex >= structEntries.Length)
            {
                throw new FormatException($"Struct index {structIndex} is out of range.");
            }

            if (++_structDepth > MaxStructDepth)
            {
                throw new FormatException("GFF struct nesting exceeds the supported depth (possible cyclic data).");
            }

            try
            {
                var (type, dataOrOffset, fieldCount) = structEntries[structIndex];
                var result = new GffStruct(type);
                foreach (var fieldIndex in ResolveFieldIndices(dataOrOffset, fieldCount))
                {
                    result.Add(ReadField(fieldIndex));
                }

                return result;
            }
            finally
            {
                _structDepth--;
            }
        }

        private IEnumerable<uint> ResolveFieldIndices(uint dataOrOffset, uint fieldCount)
        {
            if (fieldCount == 0)
            {
                yield break;
            }

            if (fieldCount == 1) { yield return dataOrOffset; yield break; }

            if (dataOrOffset % 4 != 0)
            {
                throw new FormatException($"Struct field-index offset {dataOrOffset} is not 4-byte aligned.");
            }

            var start = dataOrOffset / 4;
            if (start + fieldCount > fieldIndices.Length)
            {
                throw new FormatException($"Struct references {fieldCount} field indices starting at {start}, but only {fieldIndices.Length} exist.");
            }

            for (var i = 0; i < fieldCount; i++)
            {
                yield return fieldIndices[start + i];
            }
        }

        private GffField ReadField(uint fieldIndex)
        {
            if (fieldIndex >= fieldEntries.Length)
            {
                throw new FormatException($"Field index {fieldIndex} is out of range.");
            }

            var (typeCode, labelIndex, dataOrOffset) = fieldEntries[fieldIndex];
            if (labelIndex >= labels.Length)
            {
                throw new FormatException($"Field {fieldIndex} references label {labelIndex}, but only {labels.Length} labels exist.");
            }

            var label = labels[labelIndex];
            var type = (GffFieldType)typeCode;

            object value = type switch
            {
                GffFieldType.Byte => (byte)dataOrOffset,
                GffFieldType.Char => unchecked((sbyte)dataOrOffset),
                GffFieldType.Word => (ushort)dataOrOffset,
                GffFieldType.Short => unchecked((short)dataOrOffset),
                GffFieldType.Dword => dataOrOffset,
                GffFieldType.Int => unchecked((int)dataOrOffset),
                GffFieldType.Float => BitConverter.Int32BitsToSingle(unchecked((int)dataOrOffset)),
                GffFieldType.Dword64 => ReadFieldData(dataOrOffset, r => r.ReadUInt64(), 8),
                GffFieldType.Int64 => ReadFieldData(dataOrOffset, r => r.ReadInt64(), 8),
                GffFieldType.Double => ReadFieldData(dataOrOffset, r => r.ReadDouble(), 8),
                GffFieldType.String => ReadFieldData(dataOrOffset, ReadCExoString, minLength: 4),
                GffFieldType.ResRef => ReadFieldData(dataOrOffset, ReadResRefValue, minLength: 1),
                GffFieldType.LocString => ReadFieldData(dataOrOffset, ReadLocStringValue, minLength: 8),
                GffFieldType.Void => ReadFieldData(dataOrOffset, ReadVoidValue, minLength: 4),
                GffFieldType.Struct => ReadStruct(dataOrOffset),
                GffFieldType.List => ReadList(dataOrOffset),
                _ => throw new FormatException($"Field '{label}' has an unsupported GFF field type code {typeCode}."),
            };
            return new GffField { Label = label, Type = type, Value = value };
        }

        private T ReadFieldData<T>(uint offset, Func<BinaryFormatReader, T> read, int minLength)
        {
            if (offset > fieldDataByteCount || fieldDataByteCount - offset < minLength)
            {
                throw new FormatException($"Field data offset {offset} does not leave room for a {minLength}-byte value in a {fieldDataByteCount}-byte block.");
            }

            return reader.ReadAt((int)(fieldDataOffset + offset), read);
        }

        private static string ReadCExoString(BinaryFormatReader r)
        {
            var length = r.ReadUInt32();
            return System.Text.Encoding.Latin1.GetString(r.ReadBytes(checked((int)length)));
        }

        private static string ReadResRefValue(BinaryFormatReader r)
        {
            var length = r.ReadByte();
            return System.Text.Encoding.Latin1.GetString(r.ReadBytes(length));
        }

        private static byte[] ReadVoidValue(BinaryFormatReader r)
        {
            var length = r.ReadUInt32();
            return r.ReadBytes(checked((int)length));
        }

        private static GffLocString ReadLocStringValue(BinaryFormatReader r)
        {
            _ = r.ReadUInt32(); // TotalSize: derivable from the rest, not needed to parse
            var stringRef = r.ReadUInt32();
            var count = r.ReadUInt32();
            var entries = new List<GffLocStringEntry>((int)count);
            for (var i = 0; i < count; i++)
            {
                var stringId = r.ReadUInt32();
                var length = r.ReadUInt32();
                var text = System.Text.Encoding.Latin1.GetString(r.ReadBytes(checked((int)length)));
                entries.Add(new GffLocStringEntry(stringId, text));
            }
            return new GffLocString { StringRef = stringRef, Strings = entries };
        }

        private IReadOnlyList<GffStruct> ReadList(uint offset)
        {
            if (offset % 4 != 0)
            {
                throw new FormatException($"List offset {offset} is not 4-byte aligned.");
            }

            if (offset > listIndicesByteCount || listIndicesByteCount - offset < 4)
            {
                throw new FormatException($"List offset {offset} does not leave room for a count in a {listIndicesByteCount}-byte block.");
            }

            var count = reader.ReadAt((int)(listIndicesOffset + offset), r => r.ReadUInt32());
            var neededBytes = 4L + count * 4L;
            if (neededBytes > listIndicesByteCount - offset)
            {
                throw new FormatException($"List at offset {offset} declares {count} elements, which does not fit in the list indices block.");
            }

            var result = new List<GffStruct>((int)count);
            for (var i = 0; i < count; i++)
            {
                var structIndex = reader.ReadAt((int)(listIndicesOffset + offset + 4 + i * 4), r => r.ReadUInt32());
                result.Add(ReadStruct(structIndex));
            }
            return result;
        }
    }
}
