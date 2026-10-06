using Nwn.Authoring.Documents.NimGff;
using System.Globalization;
using System.Text;
using GffFile = Nwn.Formats.Gff.GffDocument;
using GffField = Nwn.Formats.Gff.GffField;
using GffStruct = Nwn.Formats.Gff.GffStruct;
using CExoLocString = Nwn.Formats.Gff.GffLocString;

namespace Nwn.Authoring.Documents.NimGff
{
    /// <summary>
    /// Converts the standalone formats library's read-only binary GFF model into the
    /// nwn_gff JSON document model used by the editors.
    /// </summary>
    public static class NativeGffBridge
    {
        /// <summary>Converts the shared editable document to the native resource model for host-specific storage.</summary>
        public static GffFile ToNativeDocument(JsonGffDocument document, string fileVersion = "V3.2")
        {
            ArgumentNullException.ThrowIfNull(document);
            return new GffFile { FileType = document.DataType, FileVersion = fileVersion, Root = ToNativeStruct(document.Root) };
        }

        private static GffStruct ToNativeStruct(JsonGffStruct source)
        {
            var id = source.RawStructId is null ? uint.MaxValue : uint.Parse(Encoding.ASCII.GetString(source.RawStructId), CultureInfo.InvariantCulture);
            var result = new GffStruct(id);
            foreach (var (label, field) in source.Entries)
                result.Add(ToNativeField(label, field));
            return result;
        }

        private static GffField ToNativeField(string label, JsonGffField source)
        {
            object value = source.Type switch
            {
                GffFieldType.Byte => checked((byte)source.GetUnsignedInteger()),
                GffFieldType.Char => checked((sbyte)source.GetInteger()),
                GffFieldType.Word => checked((ushort)source.GetUnsignedInteger()),
                GffFieldType.Short => checked((short)source.GetInteger()),
                GffFieldType.Dword => checked((uint)source.GetUnsignedInteger()),
                GffFieldType.Int => checked((int)source.GetInteger()),
                GffFieldType.Dword64 => source.GetUnsignedInteger(),
                GffFieldType.Int64 => source.GetInteger(),
                GffFieldType.Float => source.GetSingle(),
                GffFieldType.Double => source.GetDouble(),
                GffFieldType.CExoString or GffFieldType.ResRef => source.GetString(),
                GffFieldType.Void => JsonStringCodec.DecodeToBytes(source.RawValue ?? throw new FormatException("A void field has no value.")),
                GffFieldType.CExoLocString => new CExoLocString
                {
                    StringRef = source.GetLocStringId() ?? NoStrRefSentinel,
                    Strings = (source.LocStringEntries ?? [])
                        .Select(entry => new Nwn.Formats.Gff.GffLocStringEntry(uint.Parse(entry.LanguageKey, CultureInfo.InvariantCulture), entry.GetText())).ToArray()
                },
                GffFieldType.Struct => ToNativeStruct(source.Struct ?? throw new FormatException("A structure field has no structure.")),
                GffFieldType.List => source.GetListElements().Select(ToNativeStruct).ToArray(),
                _ => throw new FormatException("The editable field has an unsupported native type.")
            };
            return new GffField { Label = label, Type = (Nwn.Formats.Gff.GffFieldType)source.Type, Value = value };
        }

        /// <summary>The GFF "no strref" sentinel for a locstring's <c>StrRef</c>.</summary>
        private const uint NoStrRefSentinel = 0xFFFFFFFF;

        /// <summary>
        /// Encodes text as a raw JSON string token. Canonical module text stays CP-1252 when
        /// possible; text decoded from another NWN language codepage falls back to UTF-8 rather
        /// than being rejected or replaced.
        /// </summary>
        private static byte[] EncodeNwnString(string value)
        {
            try
            {
                return JsonStringCodec.Encode(value, UseUtf8Text.Value);
            }
            catch (EncoderFallbackException) when (!UseUtf8Text.Value)
            {
                return JsonStringCodec.Encode(value, useUtf8: true);
            }
        }

        /// <summary>
        /// Per-conversion text-encoding choice for <see cref="ToJsonDocument(GffFile, bool)"/>.
        /// AsyncLocal so nested/concurrent conversions cannot observe each other's flag.
        /// </summary>
        private static readonly AsyncLocal<bool> UseUtf8Text = new();
        private static readonly AsyncLocal<bool> PreserveNativeFieldOrder = new();

        /// <summary>
        /// Converts a parsed binary GFF into a JSON document. The result is brand new and owned by nobody,
        /// so the conversion runs as construction - see <see cref="Nwn.Authoring.Editing.EditScope.EnterConstruction"/>
        /// for why that is not merely an optimisation.
        /// </summary>
        public static JsonGffDocument ToJsonDocument(GffFile file) => ToJsonDocument(file, encodeTextAsUtf8: false);

        /// <summary>
        /// Converts with an explicit text-encoding choice. Pass true when the strings being
        /// re-emitted came from a UTF-8 source document so its tokens round-trip byte-identically;
        /// the default stays Windows-1252, the module's canonical storage.
        /// </summary>
        public static JsonGffDocument ToJsonDocument(GffFile file, bool encodeTextAsUtf8)
            => ToJsonDocument(file, encodeTextAsUtf8, preserveNativeFieldOrder: false);

        /// <summary>Preserves the source native field order when the host's storage requires it.</summary>
        public static JsonGffDocument ToJsonDocument(GffFile file, bool encodeTextAsUtf8, bool preserveNativeFieldOrder)
        {
            var previousEncoding = UseUtf8Text.Value;
            var previousOrder = PreserveNativeFieldOrder.Value;
            UseUtf8Text.Value = encodeTextAsUtf8;
            PreserveNativeFieldOrder.Value = preserveNativeFieldOrder;
            try
            {
                return ToJsonDocumentCore(file);
            }
            finally
            {
                UseUtf8Text.Value = previousEncoding;
                PreserveNativeFieldOrder.Value = previousOrder;
            }
        }

        private static JsonGffDocument ToJsonDocumentCore(GffFile file)
        {
            using var construction = Nwn.Authoring.Editing.EditScope.EnterConstruction();

            var root = new JsonGffStruct();
            if (PreserveNativeFieldOrder.Value && file.Root.StructId != uint.MaxValue)
                root.SetStructId(file.Root.StructId);
            foreach (var field in file.Root.Fields)
                AddField(root, field.Label, ConvertFieldToJson(field));

            return new JsonGffDocument(file.FileType, root);
        }

        private static JsonGffStruct ConvertNestedStructToJson(GffStruct source)
        {
            var target = new JsonGffStruct { RawStructId = EncodeUInt64(source.StructId) };
            foreach (var field in source.Fields)
                AddField(target, field.Label, ConvertFieldToJson(field));

            return target;
        }

        private static void AddField(JsonGffStruct target, string label, JsonGffField field)
        {
            if (PreserveNativeFieldOrder.Value) target.AppendParsed(label, field);
            else target.Add(label, field);
        }

        private static JsonGffField ConvertFieldToJson(GffField field)
        {
            switch (field.Type)
            {
                case Nwn.Formats.Gff.GffFieldType.Byte:
                    return JsonGffField.CreateScalar(GffFieldType.Byte, EncodeUInt64((byte)field.Value!));
                case Nwn.Formats.Gff.GffFieldType.Char:
                    return JsonGffField.CreateScalar(GffFieldType.Char, EncodeInt64((sbyte)field.Value!));
                case Nwn.Formats.Gff.GffFieldType.Word:
                    return JsonGffField.CreateScalar(GffFieldType.Word, EncodeUInt64((ushort)field.Value!));
                case Nwn.Formats.Gff.GffFieldType.Short:
                    return JsonGffField.CreateScalar(GffFieldType.Short, EncodeInt64((short)field.Value!));
                case Nwn.Formats.Gff.GffFieldType.Dword:
                    return JsonGffField.CreateScalar(GffFieldType.Dword, EncodeUInt64((uint)field.Value!));
                case Nwn.Formats.Gff.GffFieldType.Int:
                    return JsonGffField.CreateScalar(GffFieldType.Int, EncodeInt64((int)field.Value!));
                case Nwn.Formats.Gff.GffFieldType.Dword64:
                    return JsonGffField.CreateScalar(GffFieldType.Dword64, EncodeUInt64((ulong)field.Value!));
                case Nwn.Formats.Gff.GffFieldType.Int64:
                    return JsonGffField.CreateScalar(GffFieldType.Int64, EncodeInt64((long)field.Value!));
                case Nwn.Formats.Gff.GffFieldType.Float:
                    // Host codecs preserving native field order also preserve every untouched
                    // binary value, so use the round-trip representation for those documents.
                    return JsonGffField.CreateScalar(
                        GffFieldType.Float,
                        Ascii(PreserveNativeFieldOrder.Value
                            ? ((float)field.Value!).ToString("R", CultureInfo.InvariantCulture)
                            : NimFloatFormatter.Format((float)field.Value!)));
                case Nwn.Formats.Gff.GffFieldType.Double:
                    return JsonGffField.CreateScalar(
                        GffFieldType.Double,
                        Ascii(PreserveNativeFieldOrder.Value
                            ? ((double)field.Value!).ToString("R", CultureInfo.InvariantCulture)
                            : NimFloatFormatter.Format((double)field.Value!)));
                case Nwn.Formats.Gff.GffFieldType.String:
                {
                    var result = JsonGffField.CreateScalar(
                        GffFieldType.CExoString,
                        EncodeNwnString(field.Value as string ?? string.Empty));
                    result.PreferUtf8Text = UseUtf8Text.Value;
                    return result;
                }
                case Nwn.Formats.Gff.GffFieldType.ResRef:
                {
                    var result = JsonGffField.CreateScalar(
                        GffFieldType.ResRef,
                        EncodeNwnString(field.Value as string ?? string.Empty));
                    result.PreferUtf8Text = UseUtf8Text.Value;
                    return result;
                }
                case Nwn.Formats.Gff.GffFieldType.Void:
                    // Void payloads may not be valid UTF-8, so bridge them at the byte level.
                    return JsonGffField.CreateScalar(GffFieldType.Void, JsonStringCodec.EncodeBytes(field.Value as byte[] ?? Array.Empty<byte>()));
                case Nwn.Formats.Gff.GffFieldType.LocString:
                    return ConvertLocStringToJson(field.Value as CExoLocString ?? new CExoLocString());
                case Nwn.Formats.Gff.GffFieldType.Struct:
                    return ConvertStructFieldToJson((GffStruct)field.Value!);
                case Nwn.Formats.Gff.GffFieldType.List:
                    return ConvertListFieldToJson((IReadOnlyList<GffStruct>)field.Value!);
                default:
                    throw new NotSupportedException($"Unsupported GFF field type '{field.Type}' on field '{field.Label}'.");
            }
        }

        private static JsonGffField ConvertLocStringToJson(CExoLocString loc)
        {
            var field = JsonGffField.CreateLocString();
            field.PreferUtf8Text = UseUtf8Text.Value;
            if (loc.StringRef != NoStrRefSentinel)
                field.RawLocStringId = EncodeUInt64(loc.StringRef);

            // Preserve the substrings' natural enumeration order (== insertion order, since we
            // never remove entries) rather than sorting by language id: real GFF data is not
            // always stored in ascending language-id order (e.g. legacy entries appended after
            // later ones), and nwn_gff round-trips whatever order the binary substring table
            // was in.
            foreach (var entry in loc.Strings)
            {
                var rawText = EncodeNwnString(entry.Text);
                field.LocStringEntries!.Add(new LocStringEntry(
                    entry.StringId.ToString(CultureInfo.InvariantCulture),
                    rawText,
                    UseUtf8Text.Value));
            }

            return field;
        }

        private static JsonGffField ConvertStructFieldToJson(GffStruct source)
        {
            var childStruct = ConvertNestedStructToJson(source);
            return new JsonGffField(GffFieldType.Struct)
            {
                RawFieldStructId = childStruct.RawStructId,
                Struct = childStruct
            };
        }

        private static JsonGffField ConvertListFieldToJson(IReadOnlyList<GffStruct> list)
        {
            var field = JsonGffField.CreateList();
            foreach (var element in list)
                field.Elements!.Add(ConvertNestedStructToJson(element));

            return field;
        }

        private static byte[] EncodeUInt64(ulong value)
        {
            return Encoding.ASCII.GetBytes(value.ToString(CultureInfo.InvariantCulture));
        }

        private static byte[] EncodeInt64(long value)
        {
            return Encoding.ASCII.GetBytes(value.ToString(CultureInfo.InvariantCulture));
        }

        private static byte[] Ascii(string text)
        {
            return Encoding.ASCII.GetBytes(text);
        }
    }
}
