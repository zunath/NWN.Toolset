#nullable enable
using System.Collections.Generic;

namespace Nwn.Authoring.Areas.Generation.Tilesets;

/// <summary>Ordered tile and orientation candidates sharing one terrain, edge, and optional height signature.</summary>
public sealed class TileCandidateSet
{
    private readonly List<(int TileId, int Orientation, int TileMin)> _all = new();
    private readonly List<(int TileId, int Orientation, int TileMin)> _fullyPathable = new();

    public IReadOnlyList<(int TileId, int Orientation, int TileMin)> All => _all;
    public IReadOnlyList<(int TileId, int Orientation, int TileMin)> FullyPathable => _fullyPathable;

    internal void Add(int tileId, int orientation, int tileMin, bool fullyPathable)
    {
        var candidate = (tileId, orientation, tileMin);
        _all.Add(candidate);
        if (fullyPathable)
            _fullyPathable.Add(candidate);
    }
}

