
namespace Nwn.Authoring.Documents.NimGff
{
    /// <summary>
    /// GFF field types as they appear in the "type" property of nwn_gff JSON documents.
    /// </summary>
    public enum GffFieldType
    {
        Byte,
        Char,
        Word,
        Short,
        Dword,
        Int,
        Dword64,
        Int64,
        Float,
        Double,
        CExoString,
        ResRef,
        CExoLocString,
        Void,
        Struct,
        List
    }

}
