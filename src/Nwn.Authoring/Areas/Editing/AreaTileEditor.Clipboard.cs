using Nwn.Authoring.Areas.Placement;
using Nwn.Authoring.Areas.Tiles;
using Nwn.Authoring.Documents.Native;
using Nwn.Formats.Tilesets;

namespace Nwn.Authoring.Areas.Editing;

public sealed partial class AreaTileEditor
{
    private IReadOnlyList<(int Column, int Row)> _selectedCells = Array.Empty<(int, int)>();
    public IReadOnlyList<(int Column, int Row)> SelectedCells => _selectedCells;

    public void SelectCells(IEnumerable<(int Column, int Row)> cells)
    {
        var are = new AreDocument(_documents.Area.Document);
        _selectedCells = Array.AsReadOnly(cells.Distinct()
            .Where(cell => AreaTiles.StateAt(are, cell.Column, cell.Row) != null).ToArray());
        _selectedCell = _selectedCells.Count == 0 ? null : _selectedCells[^1];
        StateChanged?.Invoke();
    }

    public AreaTileClipboardEntry? CopySelection()
    {
        if (_selectedCells.Count == 0)
            return null;
        var are = new AreDocument(_documents.Area.Document);
        var left = _selectedCells.Min(cell => cell.Column);
        var bottom = _selectedCells.Min(cell => cell.Row);
        var tiles = _selectedCells.Select(cell => new AreaCopiedTile(
            cell.Column - left, cell.Row - bottom,
            InstanceFieldMap.Duplicate(are.Tiles[AreaTiles.IndexOf(are, cell.Column, cell.Row)]))).ToArray();
        return new AreaTileClipboardEntry(are.Tileset ?? string.Empty,
            _selectedCells.Max(cell => cell.Column) - left + 1,
            _selectedCells.Max(cell => cell.Row) - bottom + 1, Array.AsReadOnly(tiles));
    }

    public bool CanPasteAt(AreaTileClipboardEntry copy, int column, int row)
    {
        var are = new AreDocument(_documents.Area.Document);
        return string.Equals(are.Tileset, copy.Tileset, StringComparison.OrdinalIgnoreCase) &&
               copy.Columns > 0 && copy.Rows > 0 && copy.Tiles.Count > 0 &&
               column >= 0 && row >= 0 && column + (long)copy.Columns <= AreaTiles.Width(are) &&
               row + (long)copy.Rows <= AreaTiles.Height(are) &&
               copy.Tiles.All(tile => tile.Column >= 0 && tile.Column < copy.Columns &&
                   tile.Row >= 0 && tile.Row < copy.Rows);
    }

    public AreaTileEditOutcome PasteAt(AreaTileClipboardEntry copy, int column, int row)
    {
        if (!CanPasteAt(copy, column, row))
            return AreaTileEditOutcome.Rejected;
        var are = new AreDocument(_documents.Area.Document);
        var list = _documents.Area.Document.Root.Get("Tile_List");
        var changed = _documents.ExecuteArea("Paste tiles", () =>
        {
            foreach (var tile in copy.Tiles)
            {
                var index = AreaTiles.IndexOf(are, column + tile.Column, row + tile.Row);
                var replacement = InstanceFieldMap.Duplicate(tile.Tile);
                if (are.Tiles[index].StructId is { } structId)
                    replacement.SetStructId(structId);
                list.RemoveElementAt(index);
                list.InsertElement(index, replacement);
            }
        });
        return FinishClipboardEdit(changed);
    }

    public AreaTileEditOutcome EraseSelection()
    {
        if (_selectedCells.Count == 0)
            return AreaTileEditOutcome.NoChange;
        var are = new AreDocument(_documents.Area.Document);
        var tileset = ResolveTileset(are);
        if (tileset == null || TilePainter.DefaultFillTerrain(tileset) is not { } terrain ||
            TilePainter.FindSolidTile(tileset, terrain) is not { } fill)
            return AreaTileEditOutcome.MissingTileset;
        var changed = _documents.ExecuteArea("Cut tiles", () =>
        {
            foreach (var cell in _selectedCells)
                AreaTiles.SetTile(are, cell.Column, cell.Row, fill.TileId, fill.Orientation);
        });
        return FinishClipboardEdit(changed);
    }

    public AreaTileEditOutcome CycleVariant(int column, int row)
    {
        var are = new AreDocument(_documents.Area.Document);
        var tileset = ResolveTileset(are);
        var state = AreaTiles.StateAt(are, column, row);
        if (tileset == null)
            return AreaTileEditOutcome.MissingTileset;
        if (state is not { } current || current.TileId < 0 || current.TileId >= tileset.Tiles.Count)
            return AreaTileEditOutcome.Rejected;
        var original = tileset.Tiles[current.TileId];
        var variants = new List<TileCandidate>();
        for (var id = 0; id < tileset.Tiles.Count; id++)
        for (var turn = 0; turn < 4; turn++)
        {
            var orientation = (current.Orientation + turn) % 4;
            if (!SameTopology(original, current.Orientation, tileset.Tiles[id], orientation))
                continue;
            variants.Add(new TileCandidate(id, orientation));
            break;
        }
        if (variants.Count < 2)
            return AreaTileEditOutcome.NoChange;
        var currentIndex = variants.FindIndex(candidate => candidate.TileId == current.TileId);
        var next = variants[(currentIndex + 1) % variants.Count];
        return FinishClipboardEdit(_documents.ExecuteArea("Cycle tile variant", () =>
            AreaTiles.SetTile(are, column, row, next.TileId, next.Orientation)));
    }

    private static bool SameTopology(TileDefinition a, int aOrientation, TileDefinition b, int bOrientation) =>
        Enum.GetValues<TileCorner>().All(corner =>
            string.Equals(TileAdjacency.WorldCornerTerrain(a, aOrientation, corner),
                TileAdjacency.WorldCornerTerrain(b, bOrientation, corner), StringComparison.OrdinalIgnoreCase) &&
            TileAdjacency.WorldCornerHeight(a, aOrientation, corner) ==
                TileAdjacency.WorldCornerHeight(b, bOrientation, corner)) &&
        Enum.GetValues<TileEdge>().All(edge =>
            string.Equals(TileAdjacency.WorldEdgeCrosser(a, aOrientation, edge),
                TileAdjacency.WorldEdgeCrosser(b, bOrientation, edge), StringComparison.OrdinalIgnoreCase));

    private AreaTileEditOutcome FinishClipboardEdit(bool changed)
    {
        if (!changed) return AreaTileEditOutcome.NoChange;
        InvalidateValidity();
        SceneRefreshRequested?.Invoke();
        StateChanged?.Invoke();
        return AreaTileEditOutcome.Changed;
    }
}
