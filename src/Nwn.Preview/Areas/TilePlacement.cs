// SPDX-License-Identifier: MIT

using System.Numerics;
using Nwn.Preview.Scene;

namespace Nwn.Preview.Areas
{
    public sealed class TilePlacement
    {
        /// <summary>Index into the area's Tile_List (row-major: index = row * Width + col).</summary>
        public required int TileIndex { get; init; }

        public required int Column { get; init; }
        public required int Row { get; init; }

        /// <summary>Raw Tile_ID: an index into the tileset's [TILEn] entries.</summary>
        public required int TileId { get; init; }

        /// <summary>Raw Tile_Orientation: 0-3, each step a 90-degree counter-clockwise turn.</summary>
        public required int Orientation { get; init; }

        /// <summary>Raw Tile_Height: an integer level multiplied by the tileset's Transition height.</summary>
        public required int HeightLevel { get; init; }

        /// <summary>World-space X of this tile's 10m-square center (before rotation is applied).</summary>
        public required float CenterX { get; init; }

        /// <summary>World-space Y of this tile's 10m-square center (before rotation is applied).</summary>
        public required float CenterY { get; init; }

        /// <summary>World-space Z offset: <see cref="HeightLevel"/> * the tileset's Transition height.</summary>
        public required float HeightOffset { get; init; }

        /// <summary>
        /// Full local-to-world transform for this tile's model: rotate about the tile square's
        /// center by <see cref="Orientation"/> * 90 degrees (CCW, about +Z), then translate to
        /// (<see cref="CenterX"/>, <see cref="CenterY"/>, <see cref="HeightOffset"/>).
        /// </summary>
        public required Matrix4x4 Transform { get; init; }

        /// <summary>The tile model's resref (from the tileset's TileDefinition.Model), or null when the tile id itself could not be resolved.</summary>
        public string? ModelResRef { get; init; }

        /// <summary>
        /// The shared, cached render geometry for <see cref="ModelResRef"/>, or null when this
        /// placement is a <see cref="IsFallback"/> (missing/unparseable model). Multiple
        /// placements across an area (and across a batch of areas) share the same instance.
        /// </summary>
        public RenderModel? Model { get; init; }

        /// <summary>
        /// True when the tile's model could not be resolved (bad Tile_ID, missing Model resref, or
        /// the model resource could not be found/parsed). A renderer should draw a unit-cube
        /// placeholder at <see cref="Transform"/> for these instead of skipping the tile.
        /// </summary>
        public required bool IsFallback { get; init; }

        /// <summary>
        /// This tile's walkmesh in tile-local space (apply <see cref="Transform"/> to reach world
        /// space), or null when the tile has no resolvable .wok. Shared/cached per tile-model
        /// resref like <see cref="Model"/>.
        /// </summary>
        public WalkMesh? Walkmesh { get; init; }
    }
}

