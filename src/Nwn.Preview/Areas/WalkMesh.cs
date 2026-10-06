// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Nwn.Preview.Areas
{
    /// <summary>
    /// One tile's walkmesh, in tile-LOCAL space (the same frame as the tile MDL - apply
    /// <see cref="TilePlacement.Transform"/> to reach world space).
    /// </summary>
    public sealed class WalkMesh
    {
        public required IReadOnlyList<Vector3> Vertices { get; init; }
        public required IReadOnlyList<WalkFace> Faces { get; init; }
    }
}

