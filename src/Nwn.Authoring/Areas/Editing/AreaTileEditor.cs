using Nwn.Authoring.Areas.Tiles;
using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Editing;
using Nwn.Formats.Tilesets;

namespace Nwn.Authoring.Areas.Editing;

/// <summary>Coordinates tile selection, placement, paint validity and edits for one area session.</summary>
public sealed class AreaTileEditor
{
    private readonly AreaDocumentEditSession _documents;
    private readonly Func<string, TilesetDefinition?> _resolveTileset;
    private readonly Dictionary<(int Column, int Row), bool> _cellValidity = new();
    private readonly Dictionary<(int Column, int Row, bool Vertical), bool> _edgeValidity = new();
    private TilePaletteEntry? _armedEntry;
    private int _armedOrientation;
    private (int Column, int Row)? _selectedCell;

    public TilePaletteEntry? ArmedEntry => _armedEntry;
    public int ArmedOrientation => _armedOrientation;
    public bool IsPlacementArmed => _armedEntry != null;
    public bool IsTerrainBrush => !string.IsNullOrEmpty(_armedEntry?.Terrain);
    public bool IsCrosserBrush => _armedEntry?.Crosser != null;
    public bool TargetsVertex => IsTerrainBrush;
    public bool TargetsEdge => IsCrosserBrush;
    public (int Columns, int Rows) Footprint => _armedEntry is { } entry ? (entry.Columns, entry.Rows) : (1, 1);
    public bool CanRotate => _armedEntry is { Columns: 1, Rows: 1, Terrain: null, Crosser: null };
    public (int Column, int Row)? SelectedCell => _selectedCell;
    public event Action? StateChanged;
    public event Action? PaintRejected;
    public event Action? SceneRefreshRequested;

    public AreaTileEditor(AreaDocumentEditSession documents, Func<string, TilesetDefinition?> resolveTileset)
    {
        _documents = documents ?? throw new ArgumentNullException(nameof(documents));
        _resolveTileset = resolveTileset ?? throw new ArgumentNullException(nameof(resolveTileset));
    }

    public bool Arm(TilePaletteEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (entry.TileIds.Count == 0 || entry.Columns <= 0 || entry.Rows <= 0 ||
            entry.TileIds.Count != checked(entry.Columns * entry.Rows) ||
            (entry.Terrain != null && entry.Crosser != null))
            return false;

        _armedEntry = entry;
        _armedOrientation = 0;
        InvalidateValidity();
        StateChanged?.Invoke();
        return true;
    }

    public void Cancel()
    {
        if (_armedEntry == null)
            return;
        _armedEntry = null;
        _armedOrientation = 0;
        InvalidateValidity();
        StateChanged?.Invoke();
    }

    public bool Rotate()
    {
        if (!CanRotate)
            return false;
        _armedOrientation = (_armedOrientation + 1) % 4;
        StateChanged?.Invoke();
        return true;
    }

    public void SelectCell((int Column, int Row)? cell)
    {
        if (_selectedCell == cell)
            return;
        _selectedCell = cell;
        StateChanged?.Invoke();
    }

    public void InvalidatePlacementValidity() => InvalidateValidity();

    public AreaTileEditOutcome AdjustSelectedHeight(int delta, string description = "Adjust tile height")
    {
        if (_selectedCell is not { } cell || delta == 0)
            return AreaTileEditOutcome.NoChange;
        var are = new AreDocument(_documents.Area.Document);
        var state = AreaTiles.StateAt(are, cell.Column, cell.Row);
        if (state == null || state.Value.HeightLevel + Math.Sign(delta) < AreaTiles.MinimumHeightLevel)
            return AreaTileEditOutcome.Rejected;

        var changed = _documents.ExecuteArea(description, () =>
            AreaTiles.TryAdjustHeightLevel(are, cell.Column, cell.Row, Math.Sign(delta)));
        if (!changed)
            return AreaTileEditOutcome.NoChange;
        InvalidateValidity();
        SceneRefreshRequested?.Invoke();
        StateChanged?.Invoke();
        return AreaTileEditOutcome.Changed;
    }

    public bool CanPlaceAt(int column, int row)
    {
        if (_armedEntry == null)
            return false;
        if (_cellValidity.TryGetValue((column, row), out var valid))
            return valid;
        valid = IsValidCellPlacement(column, row, _armedEntry);
        _cellValidity[(column, row)] = valid;
        return valid;
    }

    public bool CanPaintCrosserAt(int column, int row, bool vertical)
    {
        if (_armedEntry?.Crosser is not { } crosser)
            return false;
        if (_edgeValidity.TryGetValue((column, row, vertical), out var valid))
            return valid;
        var are = new AreDocument(_documents.Area.Document);
        var tileset = ResolveTileset(are);
        valid = tileset != null && TilePainter.CanPaintCrosserEdge(
            tileset, AreaTiles.Width(are), AreaTiles.Height(are), AreaTiles.StateReader(are),
            column, row, vertical, crosser);
        _edgeValidity[(column, row, vertical)] = valid;
        return valid;
    }

    public AreaTileEditOutcome CommitAt(int column, int row, string? description = null)
    {
        if (_armedEntry is not { } entry || entry.Crosser != null)
            return AreaTileEditOutcome.NoChange;
        if (!string.IsNullOrEmpty(entry.Terrain))
            return CommitTerrain(column, row, entry, entry.Terrain, description);

        var are = new AreDocument(_documents.Area.Document);
        var writes = new List<(int Column, int Row, int TileId)>();
        for (var localRow = 0; localRow < entry.Rows; localRow++)
        for (var localColumn = 0; localColumn < entry.Columns; localColumn++)
        {
            var tileId = entry.TileIds[localRow * entry.Columns + localColumn];
            if (tileId < 0)
                continue;
            var targetColumn = column + localColumn;
            var targetRow = row + localRow;
            if (targetColumn < 0 || targetRow < 0 || targetColumn >= AreaTiles.Width(are) || targetRow >= AreaTiles.Height(are))
                return AreaTileEditOutcome.OutOfBounds;
            writes.Add((targetColumn, targetRow, tileId));
        }
        if (writes.Count == 0)
            return AreaTileEditOutcome.NoChange;

        var orientation = CanRotate ? _armedOrientation : 0;
        _armedEntry = null;
        _armedOrientation = 0;
        StateChanged?.Invoke();
        var changed = _documents.ExecuteArea(description ?? "Place tile", () =>
        {
            foreach (var write in writes)
                AreaTiles.SetTile(are, write.Column, write.Row, write.TileId, orientation);
        });
        if (!changed)
            return AreaTileEditOutcome.NoChange;
        InvalidateValidity();
        SceneRefreshRequested?.Invoke();
        StateChanged?.Invoke();
        return AreaTileEditOutcome.Changed;
    }

    public AreaTileEditOutcome CommitCrosserAt(int column, int row, bool vertical, string? description = null)
    {
        if (_armedEntry is not { Crosser: { } crosser } entry)
            return AreaTileEditOutcome.NoChange;
        var are = new AreDocument(_documents.Area.Document);
        var tileset = ResolveTileset(are);
        if (tileset == null)
            return AreaTileEditOutcome.MissingTileset;
        var changes = TilePainter.PaintCrosserEdge(
            tileset, AreaTiles.Width(are), AreaTiles.Height(are), AreaTiles.StateReader(are),
            column, row, vertical, crosser);
        if (changes.Count == 0)
        {
            if (!TilePainter.CanPaintCrosserEdge(
                    tileset, AreaTiles.Width(are), AreaTiles.Height(are), AreaTiles.StateReader(are),
                    column, row, vertical, crosser))
                PaintRejected?.Invoke();
            return AreaTileEditOutcome.NoChange;
        }
        var changed = _documents.ExecuteArea(description ?? "Paint crosser", () => ApplyChanges(are, changes));
        if (!changed)
            return AreaTileEditOutcome.NoChange;
        InvalidateValidity();
        SceneRefreshRequested?.Invoke();
        StateChanged?.Invoke();
        return AreaTileEditOutcome.Changed;
    }

    private AreaTileEditOutcome CommitTerrain(
        int column,
        int row,
        TilePaletteEntry entry,
        string terrain,
        string? description)
    {
        var are = new AreDocument(_documents.Area.Document);
        var tileset = ResolveTileset(are);
        if (tileset == null)
            return AreaTileEditOutcome.MissingTileset;
        var changes = TilePainter.PaintTerrainVertex(
            tileset, AreaTiles.Width(are), AreaTiles.Height(are), AreaTiles.StateReader(are), column, row, terrain);
        if (changes.Count == 0)
        {
            if (!TilePainter.CanPaintTerrainVertex(
                    tileset, AreaTiles.Width(are), AreaTiles.Height(are), AreaTiles.StateReader(are), column, row, terrain))
                PaintRejected?.Invoke();
            return AreaTileEditOutcome.NoChange;
        }
        var changed = _documents.ExecuteArea(description ?? $"Paint terrain: {entry.Label}", () => ApplyChanges(are, changes));
        if (!changed)
            return AreaTileEditOutcome.NoChange;
        InvalidateValidity();
        SceneRefreshRequested?.Invoke();
        StateChanged?.Invoke();
        return AreaTileEditOutcome.Changed;
    }

    private bool IsValidCellPlacement(int column, int row, TilePaletteEntry entry)
    {
        var are = new AreDocument(_documents.Area.Document);
        var width = AreaTiles.Width(are);
        var height = AreaTiles.Height(are);
        if (!string.IsNullOrEmpty(entry.Terrain))
        {
            if (column < 0 || row < 0 || column > width || row > height)
                return false;
            var tileset = ResolveTileset(are);
            return tileset != null && TilePainter.CanPaintTerrainVertex(
                tileset, width, height, AreaTiles.StateReader(are), column, row, entry.Terrain);
        }
        return column >= 0 && row >= 0 && column < width && row < height &&
               column + entry.Columns <= width && row + entry.Rows <= height;
    }

    private TilesetDefinition? ResolveTileset(AreDocument are) =>
        string.IsNullOrWhiteSpace(are.Tileset) ? null : _resolveTileset(are.Tileset);

    private static void ApplyChanges(AreDocument are, IReadOnlyList<TilePaintChange> changes)
    {
        foreach (var change in changes)
            AreaTiles.SetTile(are, change.Col, change.Row, change.TileId, change.Orientation);
    }

    private void InvalidateValidity()
    {
        _cellValidity.Clear();
        _edgeValidity.Clear();
    }
}
