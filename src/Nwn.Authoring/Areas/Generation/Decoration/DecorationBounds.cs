#nullable disable
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Nwn.Authoring.Areas.Generation.Decoration
{
    /// <summary>Stores already-scaled, axis-aligned world bounds for a measured building footprint.</summary>
    public readonly record struct DecorationBounds(float MinX, float MinY, float MaxX, float MaxY)
    {
        /// <summary>Checks two building rectangles for an interior overlap beyond the placement tolerance.</summary>
        public bool Overlaps(DecorationBounds other) =>
            MinX < other.MaxX - 0.001f && MaxX > other.MinX + 0.001f &&
            MinY < other.MaxY - 0.001f && MaxY > other.MinY + 0.001f;

        /// <summary>Checks whether a circle intrudes into this building rectangle.</summary>
        public bool IntersectsCircle(float x, float y, float radius)
        {
            var dx = x - Math.Clamp(x, MinX, MaxX);
            var dy = y - Math.Clamp(y, MinY, MaxY);
            return dx * dx + dy * dy < radius * radius;
        }
    }
}
