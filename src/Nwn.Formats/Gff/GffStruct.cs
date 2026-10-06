namespace Nwn.Formats.Gff;

/// <summary>A GFF struct: an engine-defined <see cref="StructId"/> tag (its meaning is context-
/// specific -- e.g. item property list elements use a small set of well-known IDs; the top-level
/// struct is conventionally, but not enforced, 0xFFFFFFFF) plus an ordered field list. Field order
/// is preserved on read and reproduced on write, since GFF field order is part of a file's identity
/// for byte-identical rewrites.</summary>
public sealed class GffStruct(uint structId = 0xFFFFFFFF)
{
    public uint StructId { get; set; } = structId;
    public List<GffField> Fields { get; } = [];

    public GffStruct Add(GffField field)
    {
        if (Fields.Any(f => f.Label == field.Label))
        {
            throw new InvalidOperationException($"Struct already has a field labeled '{field.Label}'.");
        }

        Fields.Add(field);
        return this;
    }

    public GffField? Find(string label) => Fields.FirstOrDefault(f => f.Label == label);
}
