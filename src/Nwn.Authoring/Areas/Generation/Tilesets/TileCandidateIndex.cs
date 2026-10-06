#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nwn.Authoring.Areas.Generation.Tilesets;

/// <summary>Indexes tile inventory by rotated terrain, edge, and optional normalized-height signatures.</summary>
public sealed class TileCandidateIndex
{
    private readonly Dictionary<string, TileCandidateSet> _lookup;

    private TileCandidateIndex(Dictionary<string, TileCandidateSet> lookup)
    {
        _lookup = lookup;
    }

    public static TileCandidateIndex Build(
        TilesetModel tileset,
        bool heightAware,
        IReadOnlyCollection<string>? extraDoorSlotCrossers = null,
        IReadOnlyCollection<int>? excludedTiles = null)
    {
        ArgumentNullException.ThrowIfNull(tileset);

        var lookup = new Dictionary<string, TileCandidateSet>();

        foreach (var tile in tileset.Tiles)
        {
            if (tile.GroupIndex != -1) continue;

            if (excludedTiles != null && excludedTiles.Count > 0 && excludedTiles.Contains(tile.TileId))
                continue;

            var isFlat = tile.CornerHeights[0] == 0 && tile.CornerHeights[1] == 0 &&
                         tile.CornerHeights[2] == 0 && tile.CornerHeights[3] == 0;

            if (!heightAware && !isFlat) continue;

            var tileMin = isFlat
                ? 0
                : Math.Min(Math.Min(tile.CornerHeights[0], tile.CornerHeights[1]),
                    Math.Min(tile.CornerHeights[2], tile.CornerHeights[3]));

            var hasCrosser = tile.HasAnyCrosser;
            var hasDoors = tile.Doors.Count != 0;
            if (hasDoors && !hasCrosser) continue;

            var fullyPathable = string.Equals(tile.PathNode, "A", StringComparison.OrdinalIgnoreCase);

            for (var orientation = 0; orientation < 4; orientation++)
            {
                var tl = tile.GetCornerAt(orientation, CornerSlot.TopLeft);
                var tr = tile.GetCornerAt(orientation, CornerSlot.TopRight);
                var br = tile.GetCornerAt(orientation, CornerSlot.BottomRight);
                var bl = tile.GetCornerAt(orientation, CornerSlot.BottomLeft);

                var top = tile.GetEdgeAt(orientation, EdgeSlot.Top);
                var right = tile.GetEdgeAt(orientation, EdgeSlot.Right);
                var bottom = tile.GetEdgeAt(orientation, EdgeSlot.Bottom);
                var left = tile.GetEdgeAt(orientation, EdgeSlot.Left);

                if (hasCrosser && hasDoors)
                {
                    var hasDoorwayEdge =
                        IsDoorway(top) || IsDoorway(right) || IsDoorway(bottom) || IsDoorway(left);
                    var hasBridgeEdge =
                        IsBridge(top) || IsBridge(right) || IsBridge(bottom) || IsBridge(left);
                    var hasExtraEdge = extraDoorSlotCrossers != null && extraDoorSlotCrossers.Count > 0 &&
                        (IsExtra(top, extraDoorSlotCrossers) || IsExtra(right, extraDoorSlotCrossers) ||
                         IsExtra(bottom, extraDoorSlotCrossers) || IsExtra(left, extraDoorSlotCrossers));
                    if (!hasDoorwayEdge && !hasBridgeEdge && !hasExtraEdge) continue;
                }

                string key;
                if (heightAware)
                {
                    var dTl = tile.GetCornerHeightAt(orientation, CornerSlot.TopLeft) - tileMin;
                    var dTr = tile.GetCornerHeightAt(orientation, CornerSlot.TopRight) - tileMin;
                    var dBr = tile.GetCornerHeightAt(orientation, CornerSlot.BottomRight) - tileMin;
                    var dBl = tile.GetCornerHeightAt(orientation, CornerSlot.BottomLeft) - tileMin;
                    key = CreateHeightAwareKey(tl, tr, br, bl, top, right, bottom, left, dTl, dTr, dBr, dBl);
                }
                else
                {
                    key = CreateKey(tl, tr, br, bl, top, right, bottom, left);
                }

                if (!lookup.TryGetValue(key, out var set))
                {
                    set = new TileCandidateSet();
                    lookup[key] = set;
                }

                set.Add(tile.TileId, orientation, tileMin, fullyPathable);
            }
        }

        return new TileCandidateIndex(lookup);
    }

    public bool TryGetCandidates(string key, out TileCandidateSet candidates)
    {
        return _lookup.TryGetValue(key, out candidates!);
    }

    public static string CreateKey(
        string? tl, string? tr, string? br, string? bl,
        string? top, string? right, string? bottom, string? left)
    {
        var cornerPart = string.Join(
            "|",
            (tl ?? string.Empty).ToUpperInvariant(),
            (tr ?? string.Empty).ToUpperInvariant(),
            (br ?? string.Empty).ToUpperInvariant(),
            (bl ?? string.Empty).ToUpperInvariant());

        var edgePart = string.Join(
            "|",
            (top ?? string.Empty).ToUpperInvariant(),
            (right ?? string.Empty).ToUpperInvariant(),
            (bottom ?? string.Empty).ToUpperInvariant(),
            (left ?? string.Empty).ToUpperInvariant());

        return cornerPart + "‖" + edgePart;
    }

    public static string CreateHeightAwareKey(
        string? tl, string? tr, string? br, string? bl,
        string? top, string? right, string? bottom, string? left,
        int dTl, int dTr, int dBr, int dBl)
    {
        var baseKey = CreateKey(tl, tr, br, bl, top, right, bottom, left);
        var heightPart = string.Join("|", dTl, dTr, dBr, dBl);
        return baseKey + "‖" + heightPart;
    }

    private static bool IsDoorway(string edge) =>
        string.Equals(edge, "Doorway", StringComparison.OrdinalIgnoreCase);

    private static bool IsBridge(string edge) =>
        string.Equals(edge, "Bridge", StringComparison.OrdinalIgnoreCase);

    private static bool IsExtra(string edge, IReadOnlyCollection<string> extraCrossers)
    {
        if (string.IsNullOrEmpty(edge)) return false;
        foreach (var candidate in extraCrossers)
        {
            if (string.Equals(edge, candidate, StringComparison.OrdinalIgnoreCase)) return true;
        }

        return false;
    }
}

