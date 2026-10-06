namespace Nwn.Formats.Gff;

/// <summary>A CExoLocString field: an optional dialog.tlk/custom-tlk string reference plus zero or
/// more inline localized substrings. <see cref="StringRef"/> is <see cref="NoStringRef"/>
/// (0xFFFFFFFF) when the field carries only inline text.</summary>
public sealed class GffLocString
{
    public const uint NoStringRef = 0xFFFFFFFF;

    public uint StringRef { get; init; } = NoStringRef;
    public IReadOnlyList<GffLocStringEntry> Strings { get; init; } = [];

    public static GffLocString FromStringRef(uint stringRef) => new() { StringRef = stringRef };
    public static GffLocString FromText(int language, int gender, string text) =>
        new() { Strings = [GffLocStringEntry.FromLanguageGender(language, gender, text)] };
}
