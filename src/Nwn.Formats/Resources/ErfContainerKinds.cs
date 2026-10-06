namespace Nwn.Formats.Resources;

public static class ErfContainerKinds
{
    public static string ToFileTypeTag(this ErfContainerKind kind) => kind switch
    {
        ErfContainerKind.Erf => "ERF ",
        ErfContainerKind.Hak => "HAK ",
        ErfContainerKind.Mod => "MOD ",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    public static ErfContainerKind Parse(string fileTypeTag) => fileTypeTag switch
    {
        "ERF " => ErfContainerKind.Erf,
        "HAK " => ErfContainerKind.Hak,
        "MOD " => ErfContainerKind.Mod,
        _ => throw new FormatException($"Unrecognized ERF file type tag '{fileTypeTag}'."),
    };
}
