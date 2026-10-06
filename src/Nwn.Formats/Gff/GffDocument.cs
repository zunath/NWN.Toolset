namespace Nwn.Formats.Gff;

/// <summary>A whole GFF file: the 4-character type tag (e.g. <c>"UTC "</c>, <c>"IFO "</c>,
/// <c>"ARE "</c>) plus the root struct.</summary>
public sealed class GffDocument
{
    public required string FileType { get; init; }
    public string FileVersion { get; init; } = "V3.2";
    public required GffStruct Root { get; init; }
}
