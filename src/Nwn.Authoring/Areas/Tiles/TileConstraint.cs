using Nwn.Formats.Tilesets;

namespace Nwn.Authoring.Areas.Tiles
{
    /// <summary>
    /// A per-corner / per-edge requirement on a single grid cell: each corner terrain and edge
    /// crosser is either a required value or null (unconstrained - any value is acceptable). Used to
    /// query <see cref="SetRuleMatcher"/> for the tiles that can legally occupy the cell.
    /// </summary>
    public sealed class TileConstraint
    {
        /// <summary>The placed cell's Tile_Height; candidate corner heights are relative to it.</summary>
        public int HeightLevel { get; init; }

        public string? NorthWest { get; init; }
        public string? NorthEast { get; init; }
        public string? SouthWest { get; init; }
        public string? SouthEast { get; init; }

        public int? NorthWestHeight { get; init; }
        public int? NorthEastHeight { get; init; }
        public int? SouthWestHeight { get; init; }
        public int? SouthEastHeight { get; init; }

        public string? NorthEdge { get; init; }
        public string? EastEdge { get; init; }
        public string? SouthEdge { get; init; }
        public string? WestEdge { get; init; }

        public string? Corner(TileCorner corner) => corner switch
        {
            TileCorner.NorthWest => NorthWest,
            TileCorner.NorthEast => NorthEast,
            TileCorner.SouthWest => SouthWest,
            TileCorner.SouthEast => SouthEast,
            _ => null
        };

        public string? Edge(TileEdge edge) => edge switch
        {
            TileEdge.North => NorthEdge,
            TileEdge.East => EastEdge,
            TileEdge.South => SouthEdge,
            TileEdge.West => WestEdge,
            _ => null
        };

        public int? CornerHeight(TileCorner corner) => corner switch
        {
            TileCorner.NorthWest => NorthWestHeight,
            TileCorner.NorthEast => NorthEastHeight,
            TileCorner.SouthWest => SouthWestHeight,
            TileCorner.SouthEast => SouthEastHeight,
            _ => null
        };

        /// <summary>A copy of this constraint with one corner set (used while gathering constraints from placed neighbours).</summary>
        public TileConstraint WithCorner(TileCorner corner, string? value) => new()
        {
            HeightLevel = HeightLevel,
            NorthWest = corner == TileCorner.NorthWest ? value : NorthWest,
            NorthEast = corner == TileCorner.NorthEast ? value : NorthEast,
            SouthWest = corner == TileCorner.SouthWest ? value : SouthWest,
            SouthEast = corner == TileCorner.SouthEast ? value : SouthEast,
            NorthWestHeight = NorthWestHeight,
            NorthEastHeight = NorthEastHeight,
            SouthWestHeight = SouthWestHeight,
            SouthEastHeight = SouthEastHeight,
            NorthEdge = NorthEdge,
            EastEdge = EastEdge,
            SouthEdge = SouthEdge,
            WestEdge = WestEdge
        };
    }

}
