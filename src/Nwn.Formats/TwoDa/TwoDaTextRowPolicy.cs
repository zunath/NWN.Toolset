namespace Nwn.Formats.TwoDa;

/// <summary>Controls how text rows with a noncanonical number of cells are interpreted.</summary>
public enum TwoDaTextRowPolicy
{
    RequireExactColumnCount,
    PadMissingCellsAndJoinSurplusIntoLastColumn
}
