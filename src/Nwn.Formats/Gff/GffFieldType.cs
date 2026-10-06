namespace Nwn.Formats.Gff;

/// <summary>The GFF V3.2 field type codes. Values below <see cref="Dword64"/> that are 4 bytes or
/// smaller are stored inline in the field entry itself; everything from <see cref="Dword64"/> on
/// (except <see cref="Struct"/>, which stores a struct index directly) is stored as a byte offset
/// into a side block.</summary>
public enum GffFieldType : uint
{
    Byte = 0,
    Char = 1,
    Word = 2,
    Short = 3,
    Dword = 4,
    Int = 5,
    Dword64 = 6,
    Int64 = 7,
    Float = 8,
    Double = 9,
    String = 10,
    ResRef = 11,
    LocString = 12,
    Void = 13,
    Struct = 14,
    List = 15,
}
