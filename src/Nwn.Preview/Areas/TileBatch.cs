// SPDX-License-Identifier: MIT

using Nwn.Preview.Scene;

namespace Nwn.Preview.Areas
{
    /// <summary>One draw batch: all placements reference this model, or all are fallback placeholders.</summary>
    public sealed class TileBatch
    {
        public RenderModel? Model { get; init; }
        public required IReadOnlyList<TilePlacement> Placements { get; init; }
    }
}
