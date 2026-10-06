namespace Nwn.Formats.TwoDa;

/// <summary>Compatibility and resource limits for reading Aurora 2DA resources.</summary>
public sealed record TwoDaReadOptions
{
    public static TwoDaReadOptions Strict { get; } = new();

    public static TwoDaReadOptions EngineCompatible { get; } = new()
    {
        AllowBinary = true,
        AllowUtf8Bom = true,
        AllowDefaultHeader = true,
        TextRowPolicy = TwoDaTextRowPolicy.PadMissingCellsAndJoinSurplusIntoLastColumn
    };

    public bool AllowBinary { get; init; }

    public bool AllowUtf8Bom { get; init; }

    public bool AllowDefaultHeader { get; init; }

    public bool DecodeWindows1252WhenNotUtf8 { get; init; } = true;

    public TwoDaTextRowPolicy TextRowPolicy { get; init; } = TwoDaTextRowPolicy.RequireExactColumnCount;

    public int MaximumColumns { get; init; } = 16_384;

    public int MaximumRows { get; init; } = 4_000_000;

    public long MaximumCells { get; init; } = 32_000_000;
}
