using Nwn.Formats.Tilesets;

namespace Nwn.Authoring.Areas.Tiles
{
    /// <summary>One cell the paint tool would rewrite: the grid position and its new tile placement.</summary>
    public readonly record struct TilePaintChange(int Col, int Row, int TileId, int Orientation);

}
