using System.Text;
using Nwn.Formats.Io;

using Nwn.Formats.Resources;

namespace Nwn.Formats.Gff;

/// <summary>
/// Serializes a <see cref="GffDocument"/> to GFF V3.2 bytes. Struct/field/label/list layout is our
/// own (BioWare's own toolset ordering is not reverse-engineered or required); what matters is that
/// it round-trips: reading the bytes this writer produces always yields back an equal document, and
/// writing that document again produces the exact same bytes.
/// </summary>
public static class GffWriter
{
    private const int HeaderSize = 56;

    public static byte[] Write(GffDocument document)
    {
        if (Encoding.ASCII.GetByteCount(document.FileType) != 4)
        {
            throw new FormatException($"GFF FileType '{document.FileType}' must be exactly 4 ASCII characters.");
        }

        var builder = new Builder();
        builder.WriteStruct(document.Root);
        return builder.Build(document.FileType, document.FileVersion);
    }

    private sealed class Builder
    {
        private readonly List<(uint Type, uint DataOrOffset, uint FieldCount)> _structs = [];
        private readonly List<(uint Type, uint LabelIndex, uint DataOrOffset)> _fields = [];
        private readonly List<string> _labelOrder = [];
        private readonly Dictionary<string, uint> _labelIndex = new(StringComparer.Ordinal);
        private readonly BinaryFormatWriter _fieldData = new();
        private readonly List<uint> _fieldIndices = [];
        private readonly List<uint> _listIndices = [];

        public uint WriteStruct(GffStruct value)
        {
            var structIndex = (uint)_structs.Count;
            _structs.Add(default); // reserved; patched below once fields are known

            var fieldIndexesForThisStruct = new List<uint>(value.Fields.Count);
            foreach (var field in value.Fields)
            {
                fieldIndexesForThisStruct.Add(WriteField(field));
            }

            uint dataOrOffset = fieldIndexesForThisStruct.Count switch
            {
                0 => 0,
                1 => fieldIndexesForThisStruct[0],
                _ => AppendFieldIndices(fieldIndexesForThisStruct),
            };
            _structs[(int)structIndex] = (value.StructId, dataOrOffset, (uint)fieldIndexesForThisStruct.Count);
            return structIndex;
        }

        private uint AppendFieldIndices(List<uint> indexes)
        {
            var offset = (uint)(_fieldIndices.Count * 4);
            _fieldIndices.AddRange(indexes);
            return offset;
        }

        private uint WriteField(GffField field)
        {
            var labelIndex = GetOrAddLabel(field.Label);
            var dataOrOffset = field.Type switch
            {
                GffFieldType.Byte => field.AsByte(),
                GffFieldType.Char => unchecked((uint)(byte)field.AsChar()),
                GffFieldType.Word => field.AsWord(),
                GffFieldType.Short => unchecked((uint)(ushort)field.AsShort()),
                GffFieldType.Dword => field.AsDword(),
                GffFieldType.Int => unchecked((uint)field.AsInt()),
                GffFieldType.Float => unchecked((uint)BitConverter.SingleToInt32Bits(field.AsFloat())),
                GffFieldType.Dword64 => AppendFieldData(w => w.WriteUInt64(field.AsDword64())),
                GffFieldType.Int64 => AppendFieldData(w => w.WriteInt64(field.AsInt64())),
                GffFieldType.Double => AppendFieldData(w => w.WriteDouble(field.AsDouble())),
                GffFieldType.String => AppendFieldData(w => WriteCExoString(w, field.AsString())),
                GffFieldType.ResRef => AppendFieldData(w => WriteResRefValue(w, field.AsResRef())),
                GffFieldType.LocString => AppendFieldData(w => WriteLocStringValue(w, field.AsLocString())),
                GffFieldType.Void => AppendFieldData(w => WriteVoidValue(w, field.AsVoid())),
                GffFieldType.Struct => WriteStruct(field.AsStruct()),
                GffFieldType.List => WriteList(field.AsList()),
                _ => throw new FormatException($"Field '{field.Label}' has an unsupported GFF field type {field.Type}."),
            };
            _fields.Add(((uint)field.Type, labelIndex, dataOrOffset));
            return (uint)(_fields.Count - 1);
        }

        private uint WriteList(IReadOnlyList<GffStruct> elements)
        {
            // Compute every child struct FIRST (into local storage): WriteStruct recurses, and a
            // nested list inside a child appends its own [count, indices...] block to this same
            // shared _listIndices list. Appending this list's own block only after all of that
            // nested recursion has finished is what keeps the block contiguous -- interleaving
            // (appending count, then calling WriteStruct per element directly into _listIndices)
            // would let a child's nested list splice its block in between this list's own count and
            // its element indices.
            var childIndices = new List<uint>(elements.Count);
            foreach (var element in elements)
            {
                childIndices.Add(WriteStruct(element));
            }

            var offset = (uint)(_listIndices.Count * 4);
            _listIndices.Add((uint)elements.Count);
            _listIndices.AddRange(childIndices);
            return offset;
        }

        private uint AppendFieldData(Action<BinaryFormatWriter> write)
        {
            var offset = (uint)_fieldData.Position;
            write(_fieldData);
            return offset;
        }

        private uint GetOrAddLabel(string label)
        {
            if (_labelIndex.TryGetValue(label, out var existing))
            {
                return existing;
            }

            if (Encoding.ASCII.GetByteCount(label) > 16)
            {
                throw new FormatException($"GFF label '{label}' exceeds 16 bytes.");
            }

            var index = (uint)_labelOrder.Count;
            _labelOrder.Add(label);
            _labelIndex[label] = index;
            return index;
        }

        private static void WriteCExoString(BinaryFormatWriter w, string value)
        {
            var bytes = Encoding.Latin1.GetBytes(value);
            w.WriteUInt32((uint)bytes.Length);
            w.WriteBytes(bytes);
        }

        private static void WriteResRefValue(BinaryFormatWriter w, string value)
        {
            var bytes = Encoding.Latin1.GetBytes(value);
            if (bytes.Length > Resref.MaxLength)
            {
                throw new FormatException($"ResRef value '{value}' exceeds {Resref.MaxLength} characters.");
            }

            w.WriteByte((byte)bytes.Length);
            w.WriteBytes(bytes);
        }

        private static void WriteVoidValue(BinaryFormatWriter w, byte[] value)
        {
            w.WriteUInt32((uint)value.Length);
            w.WriteBytes(value);
        }

        private static void WriteLocStringValue(BinaryFormatWriter w, GffLocString value)
        {
            var entryBytes = value.Strings.Select(entry => (entry, Bytes: Encoding.Latin1.GetBytes(entry.Text))).ToList();
            var innerSize = 4 + 4 + entryBytes.Sum(pair => 8 + pair.Bytes.Length);
            w.WriteUInt32((uint)innerSize);
            w.WriteUInt32(value.StringRef);
            w.WriteUInt32((uint)entryBytes.Count);
            foreach (var (entry, bytes) in entryBytes)
            {
                w.WriteUInt32(entry.StringId);
                w.WriteUInt32((uint)bytes.Length);
                w.WriteBytes(bytes);
            }
        }

        public byte[] Build(string fileType, string fileVersion)
        {
            var structOffset = HeaderSize;
            var fieldOffset = structOffset + _structs.Count * 12;
            var labelOffset = fieldOffset + _fields.Count * 12;
            var fieldDataOffset = labelOffset + _labelOrder.Count * 16;
            var fieldIndicesOffset = fieldDataOffset + _fieldData.Position;
            var listIndicesOffset = fieldIndicesOffset + _fieldIndices.Count * 4;

            var w = new BinaryFormatWriter();
            w.WriteFixedAscii(fileType, 4);
            w.WriteFixedAscii(fileVersion, 4);
            w.WriteUInt32((uint)structOffset);
            w.WriteUInt32((uint)_structs.Count);
            w.WriteUInt32((uint)fieldOffset);
            w.WriteUInt32((uint)_fields.Count);
            w.WriteUInt32((uint)labelOffset);
            w.WriteUInt32((uint)_labelOrder.Count);
            w.WriteUInt32((uint)fieldDataOffset);
            w.WriteUInt32((uint)_fieldData.Position);
            w.WriteUInt32((uint)fieldIndicesOffset);
            w.WriteUInt32((uint)(_fieldIndices.Count * 4));
            w.WriteUInt32((uint)listIndicesOffset);
            w.WriteUInt32((uint)(_listIndices.Count * 4));

            foreach (var (type, dataOrOffset, fieldCount) in _structs)
            {
                w.WriteUInt32(type);
                w.WriteUInt32(dataOrOffset);
                w.WriteUInt32(fieldCount);
            }
            foreach (var (type, labelIndex, dataOrOffset) in _fields)
            {
                w.WriteUInt32(type);
                w.WriteUInt32(labelIndex);
                w.WriteUInt32(dataOrOffset);
            }
            foreach (var label in _labelOrder)
            {
                w.WriteFixedAscii(label, 16);
            }

            w.WriteBytes(_fieldData.ToArray());
            foreach (var index in _fieldIndices)
            {
                w.WriteUInt32(index);
            }

            foreach (var index in _listIndices)
            {
                w.WriteUInt32(index);
            }

            return w.ToArray();
        }
    }
}
