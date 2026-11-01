namespace Nwn.Authoring.Areas.Editing;

public sealed record AreaTileClipboardEntry(
    string Tileset, int Columns, int Rows, IReadOnlyList<AreaCopiedTile> Tiles);
