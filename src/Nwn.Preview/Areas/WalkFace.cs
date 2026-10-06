// SPDX-License-Identifier: MIT

namespace Nwn.Preview.Areas
{
    /// <summary>One walkmesh triangle: three vertex indices, a surfacemat.2da row, and whether that row is walkable.</summary>
    public readonly struct WalkFace
    {
        /// <summary>Index into the owning <see cref="WalkMesh.Vertices"/>.</summary>
        public required int A { get; init; }

        /// <summary>Index into the owning <see cref="WalkMesh.Vertices"/>.</summary>
        public required int B { get; init; }

        /// <summary>Index into the owning <see cref="WalkMesh.Vertices"/>.</summary>
        public required int C { get; init; }

        /// <summary>surfacemat.2da row id for this face.</summary>
        public required int Material { get; init; }

        /// <summary>Resolved via the caller-supplied <c>isWalkable</c> predicate at parse time.</summary>
        public required bool Walkable { get; init; }
    }
}

