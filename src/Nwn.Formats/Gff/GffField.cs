namespace Nwn.Formats.Gff;

/// <summary>
/// One labeled GFF field. <see cref="Value"/> is boxed as the CLR type that matches
/// <see cref="Type"/> exactly: byte/sbyte/ushort/short/uint/int/ulong/long/float/double for the
/// numeric types, <see cref="string"/> for String and ResRef, <see cref="GffLocString"/>,
/// <see cref="byte"/>[] for Void, <see cref="GffStruct"/> for Struct, and
/// <see cref="IReadOnlyList{GffStruct}"/> for List.
/// </summary>
public sealed class GffField
{
    public required string Label { get; init; }
    public required GffFieldType Type { get; init; }
    public required object Value { get; init; }

    private static void ValidateLabel(string label)
    {
        if (string.IsNullOrEmpty(label) || System.Text.Encoding.ASCII.GetByteCount(label) > 16)
        {
            throw new FormatException($"GFF label '{label}' must be 1-16 ASCII bytes.");
        }
    }

    public static GffField Byte(string label, byte value) => Make(label, GffFieldType.Byte, value);
    public static GffField Char(string label, sbyte value) => Make(label, GffFieldType.Char, value);
    public static GffField Word(string label, ushort value) => Make(label, GffFieldType.Word, value);
    public static GffField Short(string label, short value) => Make(label, GffFieldType.Short, value);
    public static GffField Dword(string label, uint value) => Make(label, GffFieldType.Dword, value);
    public static GffField Int(string label, int value) => Make(label, GffFieldType.Int, value);
    public static GffField Dword64(string label, ulong value) => Make(label, GffFieldType.Dword64, value);
    public static GffField Int64(string label, long value) => Make(label, GffFieldType.Int64, value);
    public static GffField Float(string label, float value) => Make(label, GffFieldType.Float, value);
    public static GffField Double(string label, double value) => Make(label, GffFieldType.Double, value);
    public static GffField String(string label, string value) => Make(label, GffFieldType.String, value);
    public static GffField Void(string label, byte[] value) => Make(label, GffFieldType.Void, value);
    public static GffField Struct(string label, GffStruct value) => Make(label, GffFieldType.Struct, value);
    public static GffField List(string label, IReadOnlyList<GffStruct> value) => Make(label, GffFieldType.List, value);
    public static GffField LocString(string label, GffLocString value) => Make(label, GffFieldType.LocString, value);

    /// <summary>A GFF ResRef field. Unlike a hak/module resource's own resref (see
    /// <see cref="Nwn.Formats.Resources.Resref"/>), a ResRef FIELD legitimately holds the empty string
    /// pervasively -- e.g. every unset <c>Script*</c> or <c>Conversation</c> field on a stock-shaped
    /// blueprint -- so only a non-empty value is validated against the resref character/length
    /// rules.</summary>
    public static GffField ResRef(string label, string value)
    {
        if (value.Length > 0 && !Nwn.Formats.Resources.Resref.TryParse(value, out _, out var error))
        {
            throw new FormatException($"GFF field '{label}': {error}");
        }

        return Make(label, GffFieldType.ResRef, value);
    }

    private static GffField Make(string label, GffFieldType type, object value)
    {
        ValidateLabel(label);
        return new GffField { Label = label, Type = type, Value = value };
    }

    public byte AsByte() => (byte)Value;
    public sbyte AsChar() => (sbyte)Value;
    public ushort AsWord() => (ushort)Value;
    public short AsShort() => (short)Value;
    public uint AsDword() => (uint)Value;
    public int AsInt() => (int)Value;
    public ulong AsDword64() => (ulong)Value;
    public long AsInt64() => (long)Value;
    public float AsFloat() => (float)Value;
    public double AsDouble() => (double)Value;
    public string AsString() => (string)Value;
    public string AsResRef() => (string)Value;
    public byte[] AsVoid() => (byte[])Value;
    public GffStruct AsStruct() => (GffStruct)Value;
    public IReadOnlyList<GffStruct> AsList() => (IReadOnlyList<GffStruct>)Value;
    public GffLocString AsLocString() => (GffLocString)Value;
}
