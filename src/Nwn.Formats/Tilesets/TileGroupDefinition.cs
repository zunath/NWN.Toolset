namespace Nwn.Formats.Tilesets
{

    /// <summary>
    /// One entry from a [GROUPn] block: a named, pre-arranged rectangle of tiles (by index into
    /// the tileset's [TILES] list) offered together in the toolset palette.
    /// </summary>
    public sealed record TileGroupDefinition(
        string Name,
        int Rows,
        int Columns,
        int? StrRef,
        IReadOnlyList<int> TileIndices);

}
