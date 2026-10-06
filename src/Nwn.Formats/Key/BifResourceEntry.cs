namespace Nwn.Formats.Key;

/// <summary>One entry in a BIF's variable resource table: where its bytes live in the BIF file, and
/// the raw type code the BIF itself records for it (a KEY entry's <see cref="KeyResourceEntry.RawTypeCode"/>
/// is the authority for what a resource "is"; this one is read only as a cross-check).</summary>
public sealed record BifResourceEntry(int Index, uint Offset, uint FileSize, uint RawTypeCode);
