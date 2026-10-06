using Nwn.Authoring.Documents.NimGff;

namespace Nwn.Authoring.Fields;

/// <summary>The value an optional lookup-backed field stores when it names no row.</summary>
public static class FieldUnsetSentinel
{
    /// <summary>The largest value of an unsigned field, or -1 for a signed one.</summary>
    public static long For(GffFieldType fieldType) => fieldType switch
    {
        GffFieldType.Byte => byte.MaxValue,
        GffFieldType.Word => ushort.MaxValue,
        GffFieldType.Dword => uint.MaxValue,
        GffFieldType.Char or GffFieldType.Short or GffFieldType.Int or GffFieldType.Int64 => -1,
        _ => -1
    };
}
