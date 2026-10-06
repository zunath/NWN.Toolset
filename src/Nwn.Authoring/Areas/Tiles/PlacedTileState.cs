using Nwn.Formats.Tilesets;

namespace Nwn.Authoring.Areas.Tiles
{

    /// <summary>
    /// A placed area tile including its base elevation. A candidate alone is sufficient for palette
    /// selection, but adjacency is only valid when Tile_Height and the .set tile's corner heights are
    /// considered together.
    /// </summary>
    public readonly record struct PlacedTileState(int TileId, int Orientation, int HeightLevel)
    {
        public TileCandidate Candidate => new(TileId, Orientation);
    }

}
