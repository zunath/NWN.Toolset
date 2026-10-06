using Nwn.Formats.Tilesets;

namespace Nwn.Authoring.Areas.Tiles
{

    /// <summary>One tile placement option: a tile index into the tileset and a 0-3 orientation.</summary>
    public readonly record struct TileCandidate(int TileId, int Orientation);

}
