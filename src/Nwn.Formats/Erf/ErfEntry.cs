using Nwn.Formats.Resources;

namespace Nwn.Formats.Erf;

/// <summary>One resource's key-table + resource-table facts, as read from (or about to be written
/// to) an ERF/HAK/MOD container. <see cref="Offset"/>/<see cref="Size"/> describe where the raw
/// bytes live in the container stream; nothing here holds the resource's actual content, so listing
/// an archive's entries never reads resource data.</summary>
public sealed record ErfEntry(Resref ResRef, ResourceType Type, uint Offset, uint Size)
{
    public string FileName => $"{ResRef.Value}.{ResourceTypes.GetExtension(Type)}";
}
