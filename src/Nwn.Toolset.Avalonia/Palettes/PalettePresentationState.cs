using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nwn.Authoring.Resources;

namespace Nwn.Toolset.Avalonia.Palettes;

/// <summary>Projects immutable host palette data into the observable state used by PaletteView.</summary>
public partial class PalettePresentationState : ObservableObject, IDisposable
{
    public const int MaxSearchResults = 200;
    public const int MaxCategoryMatches = 40;

    private readonly IPaletteActions _actions;
    private readonly Dictionary<PaletteCategoryId, PaletteCategorySnapshot> _categories = new();
    private readonly Dictionary<PaletteEntryId, PaletteEntrySnapshot> _entries = new();
    private readonly Dictionary<PaletteCategoryId, PaletteCategoryRow> _categoryRows = new();
    private readonly Dictionary<PaletteEntryId, CancellationTokenSource> _previewRequests = new();
    private readonly object _previewRequestGate = new();
    private readonly HashSet<PaletteEntryId> _failedPreviewEntries = new();
    private readonly HashSet<PaletteCategoryId> _expanded = new();
    private PaletteSnapshot _snapshot = EmptySnapshot();
    private bool _applyingSnapshot;
    private int _categoryProjectionDepth;
    private bool _disposed;
    private bool _hasAppliedInitialLayout;
    private double _tileSize = 136;
    private double _categoryProportion;
    private string? _localStatusMessage;
    private PaletteCategoryId? _lastNotifiedCategoryId;

    public PalettePresentationState(IPaletteActions actions, PaletteTexts? texts = null)
    {
        _actions = actions ?? throw new ArgumentNullException(nameof(actions));
        Texts = texts ?? PaletteTexts.English;

        SelectTypeCommand = new RelayCommand<PaletteTypeRow>(SelectType);
        ShowCustomCommand = new RelayCommand(() => _actions.SelectSource(PaletteSource.Custom));
        ShowStandardCommand = new RelayCommand(() => _actions.SelectSource(PaletteSource.Standard));
        UseAutoTilePaintCommand = new RelayCommand(() => _actions.SelectTilePaintMode(PaletteTilePaintMode.Auto));
        UseManualTilePaintCommand = new RelayCommand(() => _actions.SelectTilePaintMode(PaletteTilePaintMode.Manual));
        ToggleExpandCommand = new RelayCommand<PaletteCategoryRow>(ToggleExpand);
        GoToCategoryCommand = new RelayCommand<PaletteCategoryMatch>(GoToCategory);
        PlaceCommand = new RelayCommand<PaletteEntryRow>(Place);
        EditCommand = new RelayCommand<PaletteEntryRow>(Edit);
        EditCopyCommand = new RelayCommand<PaletteEntryRow>(EditCopy);
        DeleteTileCommand = new AsyncRelayCommand<PaletteEntryRow>(DeleteEntryAsync);
        NewBlueprintCommand = new RelayCommand(NewBlueprint);
        NewCategoryCommand = new AsyncRelayCommand(NewCategoryAsync);
        RenameCategoryCommand = new AsyncRelayCommand(RenameCategoryAsync);
        DeleteCategoryCommand = new AsyncRelayCommand(DeleteCategoryAsync);
        TogglePinCommand = new AsyncRelayCommand(TogglePinAsync);
        FileSelectedTileCommand = new RelayCommand(FileSelectedEntry);
    }

    public PaletteTexts Texts { get; }

    public ObservableCollection<PaletteTypeRow> Types { get; } = new();

    public ObservableCollection<PaletteCategoryRow> Rows { get; } = new();

    public ObservableCollection<PaletteCategoryMatch> CategoryMatches { get; } = new();

    public ObservableCollection<PaletteEntryRow> Tiles { get; } = new();

    public RelayCommand<PaletteTypeRow> SelectTypeCommand { get; }

    public RelayCommand ShowCustomCommand { get; }

    public RelayCommand ShowStandardCommand { get; }

    public RelayCommand UseAutoTilePaintCommand { get; }

    public RelayCommand UseManualTilePaintCommand { get; }

    public RelayCommand<PaletteCategoryRow> ToggleExpandCommand { get; }

    public RelayCommand<PaletteCategoryMatch> GoToCategoryCommand { get; }

    public RelayCommand<PaletteEntryRow> PlaceCommand { get; }

    public RelayCommand<PaletteEntryRow> EditCommand { get; }

    public RelayCommand<PaletteEntryRow> EditCopyCommand { get; }

    public AsyncRelayCommand<PaletteEntryRow> DeleteTileCommand { get; }

    public RelayCommand NewBlueprintCommand { get; }

    public AsyncRelayCommand NewCategoryCommand { get; }

    public AsyncRelayCommand RenameCategoryCommand { get; }

    public AsyncRelayCommand DeleteCategoryCommand { get; }

    public AsyncRelayCommand TogglePinCommand { get; }

    public RelayCommand FileSelectedTileCommand { get; }

    public PaletteMode Mode => _snapshot.Mode;

    public ModuleResourceType? SelectedType => _snapshot.SelectedType;

    public PaletteSource Source => _snapshot.Source;

    public PaletteTilePaintMode TilePaintMode => _snapshot.TilePaintMode;

    public bool HasOpenArea => _snapshot.HasOpenArea;

    public bool IsTileMode => Mode == PaletteMode.Tiles;

    public bool IsBlueprintMode => !IsTileMode;

    public bool ShowsSourceSwitch => IsBlueprintMode && _snapshot.Capabilities.CanSelectSource;

    public bool ShowsTilePaintSwitch => IsTileMode && _snapshot.Capabilities.CanSelectTilePaintMode;

    public bool IsCustomSource => Source == PaletteSource.Custom;

    public bool IsStandardSource => Source == PaletteSource.Standard;

    public bool IsAutoTilePaint => TilePaintMode == PaletteTilePaintMode.Auto;

    public bool IsManualTilePaint => TilePaintMode == PaletteTilePaintMode.Manual;

    public bool NeedsOpenArea => IsTileMode && !HasOpenArea;

    public bool IsSearching => !string.IsNullOrWhiteSpace(Query);

    public bool HasCategoryMatches => CategoryMatches.Count > 0;

    public string CategoryMatchesLabel => Texts.Get(PaletteStringId.SearchMatches, CategoryMatches.Count);

    public bool CanCreateBlueprint => SelectedRow?.Capabilities.CanCreateBlueprint == true;

    public bool CanCreateCategory => SelectedRow?.Capabilities.CanCreateCategory == true;

    public bool CanRenameCategory => SelectedRow?.Capabilities.CanRename == true;

    public bool CanDeleteCategory => SelectedRow?.Capabilities.CanDelete == true;

    public bool CanPinCategory => SelectedRow?.Capabilities.CanPin == true;

    public bool CanFileSelectedEntry => SelectedRow?.Capabilities.CanFileSelectedEntry == true;

    public bool CanEditSelected => SelectedTile?.Snapshot.Capabilities.CanEdit == true;

    public bool CanDeleteSelected => SelectedTile?.Snapshot.Capabilities.CanDelete == true;

    public bool CanEditCopy => SelectedTile?.Snapshot.Capabilities.CanEditCopy == true;

    public bool HasBlueprintActions => SelectedTile?.Snapshot.Capabilities.HasActions == true;

    public string? ReadOnlyNotice => SelectedTile?.Snapshot.Capabilities.ReadOnlyNotice
        ?? SelectedRow?.Capabilities.ReadOnlyNotice;

    public bool HasReadOnlyNotice => !string.IsNullOrWhiteSpace(ReadOnlyNotice);

    public string NewBlueprintLabel => SelectedType is { } type
        ? _snapshot.Types.FirstOrDefault(option => option.Type == type)?.NewBlueprintLabel ?? Texts.EditBlueprint
        : Texts.EditBlueprint;

    public string Breadcrumb { get; private set; } = string.Empty;

    public string? StatusMessage => _localStatusMessage ?? _snapshot.StatusMessage;

    public double TileSize
    {
        get => _tileSize;
        set
        {
            var bounded = Math.Clamp(value, 96, 200);
            if (SetProperty(ref _tileSize, bounded))
            {
                OnPropertyChanged(nameof(TileSizeLabel));
                OnPropertyChanged(nameof(PreviewHeight));
                if (!_applyingSnapshot)
                {
                    _actions.SetTileSize(bounded);
                }
            }
        }
    }

    public string TileSizeLabel => TileSize switch
    {
        < 120 => "S",
        < 165 => "M",
        _ => "L"
    };

    public double PreviewHeight => Math.Round(TileSize * 0.72);

    public double CategoryProportion
    {
        get => _categoryProportion;
        set
        {
            var bounded = value is > 0 and < 1 ? value : 0;
            if (SetProperty(ref _categoryProportion, bounded))
            {
                if (!_applyingSnapshot)
                {
                    _actions.SetCategoryProportion(bounded);
                }
            }
        }
    }

    [ObservableProperty]
    private string _query = string.Empty;

    [ObservableProperty]
    private PaletteCategoryRow? _selectedRow;

    [ObservableProperty]
    private PaletteEntryRow? _selectedTile;

    public void SetSnapshot(PaletteSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ObjectDisposedException.ThrowIf(_disposed, this);
        ValidateSnapshot(snapshot);

        CancelAllPreviewRequests();
        if (snapshot.Revision != _snapshot.Revision)
        {
            _failedPreviewEntries.Clear();
        }

        var previousCategoryId = SelectedRow?.Id;
        var previousEntryId = SelectedTile?.Id;
        _snapshot = snapshot;
        _categories.Clear();
        _entries.Clear();
        _categoryRows.Clear();

        foreach (var category in Flatten(snapshot.Categories))
        {
            _categories.Add(category.Id, category);
        }

        foreach (var entry in snapshot.Entries)
        {
            _entries.Add(entry.Id, entry);
        }

        _applyingSnapshot = true;
        try
        {
            if (!_hasAppliedInitialLayout)
            {
                _tileSize = Math.Clamp(snapshot.InitialTileSize, 96, 200);
                _categoryProportion = snapshot.CategoryProportion is > 0 and < 1
                    ? snapshot.CategoryProportion
                    : 0;
                _hasAppliedInitialLayout = true;
                OnPropertyChanged(nameof(TileSize));
                OnPropertyChanged(nameof(TileSizeLabel));
                OnPropertyChanged(nameof(PreviewHeight));
                OnPropertyChanged(nameof(CategoryProportion));
            }

            Types.Clear();
            foreach (var option in snapshot.Types)
            {
                Types.Add(new PaletteTypeRow(
                    option,
                    option.IsTiles
                        ? snapshot.Mode == PaletteMode.Tiles
                        : snapshot.Mode == PaletteMode.Blueprints && option.Type == snapshot.SelectedType));
            }

            SelectedRow = null;
            SelectedTile = null;
            RebuildProjection(includeCategoryRows: true);
            var selectedCategoryId = previousCategoryId is { } previousId && _categories.ContainsKey(previousId)
                ? previousId
                : snapshot.InitialSelectedCategory;
            if (selectedCategoryId is { } initialCategoryId)
            {
                ExpandAncestors(initialCategoryId);
                SelectedRow = FindVisibleRow(initialCategoryId);
            }
            else
            {
                SelectedRow = null;
            }
            SelectedTile = previousEntryId is { } entryId
                ? Tiles.FirstOrDefault(entry => entry.Id == entryId)
                : null;
        }
        finally
        {
            _applyingSnapshot = false;
        }

        NotifySnapshotPropertiesChanged();
    }
    public void InvalidatePreview(PaletteEntryId id)
    {
        if (!_entries.TryGetValue(id, out var entry))
        {
            return;
        }

        CancelPreviewRequest(id);
        _failedPreviewEntries.Remove(id);
        _localStatusMessage = null;
        OnPropertyChanged(nameof(StatusMessage));
        var row = Tiles.FirstOrDefault(candidate => candidate.Id == id);
        if (row is null)
        {
            return;
        }

        row.Preview = null;
        row.PreviewRequested = false;
        EnsurePreview(row);
    }

    public void EnsurePreview(PaletteEntryRow? row)
    {
        if (_disposed || row is null || !row.Snapshot.SupportsPreview || row.PreviewRequested
            || _failedPreviewEntries.Contains(row.Id)
            || !_entries.TryGetValue(row.Id, out var current)
            || row.SnapshotRevision != _snapshot.Revision)
        {
            return;
        }

        row.PreviewRequested = true;
        CancelPreviewRequest(row.Id);
        var cancellation = new CancellationTokenSource();
        lock (_previewRequestGate)
        {
            _previewRequests[row.Id] = cancellation;
        }

        _ = LoadPreviewAsync(row, current, cancellation);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        CancelAllPreviewRequests();
    }

    partial void OnQueryChanged(string value)
    {
        OnPropertyChanged(nameof(IsSearching));
        RebuildProjection(includeCategoryRows: false);
    }

    partial void OnSelectedRowChanged(PaletteCategoryRow? value)
    {
        OnPropertyChanged(nameof(CanCreateBlueprint));
        OnPropertyChanged(nameof(CanCreateCategory));
        OnPropertyChanged(nameof(CanRenameCategory));
        OnPropertyChanged(nameof(CanDeleteCategory));
        OnPropertyChanged(nameof(CanPinCategory));
        OnPropertyChanged(nameof(CanFileSelectedEntry));
        OnPropertyChanged(nameof(ReadOnlyNotice));
        OnPropertyChanged(nameof(HasReadOnlyNotice));

        var categoryId = value?.Id;
        if (!_applyingSnapshot && _categoryProjectionDepth == 0 && categoryId != _lastNotifiedCategoryId)
        {
            _lastNotifiedCategoryId = categoryId;
            _actions.SelectCategory(categoryId);
        }

        if (_categoryProjectionDepth == 0)
        {
            RebuildEntryRows();
        }
    }

    partial void OnSelectedTileChanged(PaletteEntryRow? value)
    {
        OnPropertyChanged(nameof(CanEditSelected));
        OnPropertyChanged(nameof(CanDeleteSelected));
        OnPropertyChanged(nameof(CanEditCopy));
        OnPropertyChanged(nameof(HasBlueprintActions));
        OnPropertyChanged(nameof(ReadOnlyNotice));
        OnPropertyChanged(nameof(HasReadOnlyNotice));
    }

    private void SelectType(PaletteTypeRow? type)
    {
        if (type?.Option is { IsTiles: true } option)
        {
            _actions.SelectType(option);
            _actions.SelectMode(PaletteMode.Tiles);
            return;
        }

        if (type?.Option is { Type: not null } blueprintType)
        {
            _actions.SelectType(blueprintType);
            _actions.SelectMode(PaletteMode.Blueprints);
        }
    }

    private void ToggleExpand(PaletteCategoryRow? row)
    {
        if (row is not { HasChildren: true, Id: { } id })
        {
            return;
        }

        row.IsExpanded = !row.IsExpanded;
        if (row.IsExpanded)
        {
            _expanded.Add(id);
        }
        else
        {
            _expanded.Remove(id);
        }

        RebuildVisibleRows();
    }

    private void GoToCategory(PaletteCategoryMatch? match)
    {
        if (match is null)
        {
            return;
        }

        Query = string.Empty;
        ExpandAncestors(match.Id);
        SelectedRow = FindVisibleRow(match.Id);
    }

    private void Place(PaletteEntryRow? row)
    {
        if (row?.Snapshot.Capabilities.CanPlace == true)
        {
            _actions.Place(row.Snapshot);
        }
        else if (IsBlueprintMode && !HasOpenArea)
        {
            _localStatusMessage = Texts.OpenAreaToPlace;
            OnPropertyChanged(nameof(StatusMessage));
        }
    }

    private void Edit(PaletteEntryRow? row)
    {
        if (row?.Snapshot.Capabilities.CanEdit == true)
        {
            _actions.Edit(row.Snapshot);
        }
    }

    private void EditCopy(PaletteEntryRow? row)
    {
        if (row?.Snapshot.Capabilities.CanEditCopy == true)
        {
            _actions.EditCopy(row.Snapshot);
        }
    }

    private Task DeleteEntryAsync(PaletteEntryRow? row)
    {
        return row?.Snapshot.Capabilities.CanDelete == true
            ? _actions.DeleteAsync(row.Snapshot, CancellationToken.None)
            : Task.CompletedTask;
    }

    private void NewBlueprint()
    {
        if (SelectedRow is { } row && row.Capabilities.CanCreateBlueprint)
        {
            _actions.NewBlueprint(row.Id);
        }
    }

    private Task NewCategoryAsync()
    {
        return SelectedRow is { } row && row.Capabilities.CanCreateCategory
            ? _actions.NewCategoryAsync(row.Id, CancellationToken.None)
            : Task.CompletedTask;
    }

    private Task RenameCategoryAsync()
    {
        return SelectedRow is { Id: { } id, Capabilities.CanRename: true }
            ? _actions.RenameCategoryAsync(id, CancellationToken.None)
            : Task.CompletedTask;
    }

    private Task DeleteCategoryAsync()
    {
        return SelectedRow is { Id: { } id, Capabilities.CanDelete: true }
            ? _actions.DeleteCategoryAsync(id, CancellationToken.None)
            : Task.CompletedTask;
    }

    private Task TogglePinAsync()
    {
        return SelectedRow is { Id: { } id, Capabilities.CanPin: true }
            ? _actions.TogglePinAsync(id, CancellationToken.None)
            : Task.CompletedTask;
    }

    private void FileSelectedEntry()
    {
        if (SelectedRow is { Id: { } id, Capabilities.CanFileSelectedEntry: true })
        {
            _actions.FileSelectedEntry(id);
        }
    }

    private void RebuildProjection(bool includeCategoryRows)
    {
        if (includeCategoryRows)
        {
            RebuildCategoryRows();
        }
        RebuildCategoryMatches();
        RebuildEntryRows();
        OnPropertyChanged(nameof(HasCategoryMatches));
        OnPropertyChanged(nameof(CategoryMatchesLabel));
    }

    private void RebuildCategoryRows()
    {
        var previousSelectedId = SelectedRow?.Id;
        var allRows = new List<PaletteCategoryRow>();
        var pinned = _categories.Values
            .Where(category => category.IsPinned)
            .OrderBy(category => category.PinOrder)
            .ToList();

        foreach (var category in pinned)
        {
            var pinnedRow = CreateCategoryRow(category, 0, hasChildren: false, isPinnedRow: true);
            allRows.Add(pinnedRow);
        }

        foreach (var category in _snapshot.Categories)
        {
            AddCategoryRows(category, 0, allRows);
        }

        _categoryProjectionDepth++;
        try
        {
            _categoryRows.Clear();
            Rows.Clear();
            var hiddenBelowDepth = int.MaxValue;
            foreach (var row in allRows)
            {
                _categoryRows[row.Id] = row;
                if (row.Depth > hiddenBelowDepth)
                {
                    continue;
                }

                hiddenBelowDepth = int.MaxValue;
                Rows.Add(row);
                if (row.HasChildren && !row.IsExpanded)
                {
                    hiddenBelowDepth = row.Depth;
                }
            }

            SelectedRow = previousSelectedId is { } selectedId
                ? FindVisibleRow(selectedId)
                : null;
        }
        finally
        {
            _categoryProjectionDepth--;
        }

        NotifyCategorySelectionIfChanged(previousSelectedId);
    }

    private void AddCategoryRows(PaletteCategorySnapshot category, int depth, ICollection<PaletteCategoryRow> rows)
    {
        var row = CreateCategoryRow(category, depth, category.Children.Count > 0);
        rows.Add(row);
        foreach (var child in category.Children)
        {
            AddCategoryRows(child, depth + 1, rows);
        }
    }

    private PaletteCategoryRow CreateCategoryRow(PaletteCategorySnapshot category, int depth, bool hasChildren, bool isPinnedRow = false)
    {
        var row = new PaletteCategoryRow(category, depth, hasChildren, isPinnedRow)
        {
            IsExpanded = _expanded.Contains(category.Id)
        };
        return row;
    }

    private void RebuildVisibleRows()
    {
        var previousSelectedId = SelectedRow?.Id;
        var allRows = new List<PaletteCategoryRow>();
        foreach (var category in _categories.Values.Where(category => category.IsPinned).OrderBy(category => category.PinOrder))
        {
            allRows.Add(CreateCategoryRow(category, 0, hasChildren: false, isPinnedRow: true));
        }

        foreach (var category in _snapshot.Categories)
        {
            AddCategoryRows(category, 0, allRows);
        }

        _categoryProjectionDepth++;
        try
        {
            _categoryRows.Clear();
            Rows.Clear();
            var hiddenBelowDepth = int.MaxValue;
            foreach (var row in allRows)
            {
                _categoryRows[row.Id] = row;
                if (row.Depth > hiddenBelowDepth)
                {
                    continue;
                }

                hiddenBelowDepth = int.MaxValue;
                Rows.Add(row);
                if (row.HasChildren && !row.IsExpanded)
                {
                    hiddenBelowDepth = row.Depth;
                }
            }

            SelectedRow = previousSelectedId is { } selectedId
                ? FindVisibleRow(selectedId)
                : null;
        }
        finally
        {
            _categoryProjectionDepth--;
        }

        RebuildEntryRows();
        NotifyCategorySelectionIfChanged(previousSelectedId);
    }

    private void NotifyCategorySelectionIfChanged(PaletteCategoryId? previousSelectedId)
    {
        var categoryId = SelectedRow?.Id;
        if (_applyingSnapshot || _categoryProjectionDepth > 0
            || categoryId == previousSelectedId || categoryId == _lastNotifiedCategoryId)
        {
            return;
        }

        _lastNotifiedCategoryId = categoryId;
        _actions.SelectCategory(categoryId);
    }
    private void RebuildCategoryMatches()
    {
        CategoryMatches.Clear();
        if (IsTileMode || !IsSearching)
        {
            return;
        }

        var query = Query.Trim();
        foreach (var category in _snapshot.Categories.SelectMany(root => Flatten(new[] { root })))
        {
            if (!category.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            CategoryMatches.Add(new PaletteCategoryMatch(
                category.Id,
                category.Name,
                ParentPathFor(category.Id),
                category.Count));
            if (CategoryMatches.Count == MaxCategoryMatches)
            {
                break;
            }
        }
    }

    private void RebuildEntryRows()
    {
        CancelAllPreviewRequests();
        Tiles.Clear();
        _localStatusMessage = null;
        if (_snapshot.Mode == PaletteMode.Tiles && !_snapshot.HasOpenArea)
        {
            Breadcrumb = string.Empty;
            NotifyEntryPropertiesChanged();
            return;
        }

        var candidates = _snapshot.Entries
            .Where(entry => (_snapshot.Mode == PaletteMode.Tiles || entry.Source == _snapshot.Source)
                && (_snapshot.Mode == PaletteMode.Tiles
                    ? entry.Kind == PaletteEntryKind.Tile
                    : entry.Kind == PaletteEntryKind.Blueprint && entry.ResourceType == _snapshot.SelectedType))
            .ToList();

        if (IsSearching)
        {
            if (_snapshot.Mode == PaletteMode.Tiles)
            {
                if (SelectedRow?.Id is { } selectedTileCategory
                    && _categories.TryGetValue(selectedTileCategory, out var tileCategory))
                {
                    var selectedTileIds = DescendantEntryIds(tileCategory).ToHashSet();
                    candidates = candidates.Where(entry => selectedTileIds.Contains(entry.Id)).ToList();
                }
                else
                {
                    candidates.Clear();
                }
            }

            candidates = candidates.Where(entry => _snapshot.Mode == PaletteMode.Tiles
                    ? entry.Name.Contains(Query.Trim(), StringComparison.OrdinalIgnoreCase)
                    : entry.ResRef.Contains(Query.Trim(), StringComparison.OrdinalIgnoreCase)
                        || entry.Name.Contains(Query.Trim(), StringComparison.OrdinalIgnoreCase))
                .OrderBy(entry => entry.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            foreach (var entry in candidates.Take(MaxSearchResults))
            {
                Tiles.Add(CreateEntryRow(entry));
            }

            Breadcrumb = candidates.Count > MaxSearchResults
                ? Texts.Get(PaletteStringId.FirstSearchMatches, MaxSearchResults, candidates.Count)
                : _snapshot.Mode == PaletteMode.Tiles
                    ? Texts.Get(PaletteStringId.CategoryTileCount, string.Empty, candidates.Count)
                    : Texts.Get(PaletteStringId.BlueprintSearchMatches, candidates.Count);
            NotifyEntryPropertiesChanged();
            return;
        }

        if (SelectedRow?.Id is not { } categoryId || !_categories.TryGetValue(categoryId, out var selectedCategory))
        {
            Breadcrumb = Texts.SelectCategory;
            NotifyEntryPropertiesChanged();
            return;
        }

        var entryIds = DescendantEntryIds(selectedCategory)
            .Distinct()
            .ToHashSet();
        candidates = candidates.Where(entry => entryIds.Contains(entry.Id))
            .OrderBy(entry => entry.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        Breadcrumb = _snapshot.Mode == PaletteMode.Tiles
            ? candidates.Count > MaxSearchResults
                ? Texts.Get(PaletteStringId.CategoryTileOverflow, selectedCategory.Name, MaxSearchResults, candidates.Count)
                : Texts.Get(PaletteStringId.CategoryTileCount, selectedCategory.Name, candidates.Count)
            : ParentPathFor(categoryId);

        var visibleEntries = _snapshot.Mode == PaletteMode.Tiles
            ? candidates.Take(MaxSearchResults)
            : candidates;
        foreach (var entry in visibleEntries)
        {
            Tiles.Add(CreateEntryRow(entry));
        }

        NotifyEntryPropertiesChanged();
    }

    private PaletteEntryRow CreateEntryRow(PaletteEntrySnapshot entry)
    {
        var categoryPath = IsSearching
            ? entry.CategoryIds
                .Select(id => _categories.TryGetValue(id, out var category) ? category.Name : null)
                .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name))
            : null;
        var row = new PaletteEntryRow(entry, _snapshot.Revision)
        {
            CategoryPath = categoryPath is null ? null : Texts.Get(PaletteStringId.CategoryIn, categoryPath)
        };
        return row;
    }

    private IEnumerable<PaletteEntryId> DescendantEntryIds(PaletteCategorySnapshot category)
    {
        foreach (var id in category.EntryIds)
        {
            yield return id;
        }

        foreach (var child in category.Children)
        {
            foreach (var id in DescendantEntryIds(child))
            {
                yield return id;
            }
        }
    }

    private void ExpandAncestors(PaletteCategoryId id)
    {
        var targetPath = PathTo(id);
        foreach (var category in _categories.Values)
        {
            if (category.Children.Count == 0)
            {
                continue;
            }

            var path = PathTo(category.Id);
            if (path.Count < targetPath.Count && path.SequenceEqual(targetPath.Take(path.Count)))
            {
                _expanded.Add(category.Id);
            }
        }

        RebuildVisibleRows();
    }

    private IReadOnlyList<string> PathTo(PaletteCategoryId id)
    {
        var path = new List<string>();
        return TryBuildPath(_snapshot.Categories, id, path) ? path : Array.Empty<string>();
    }

    private bool TryBuildPath(
        IReadOnlyList<PaletteCategorySnapshot> categories,
        PaletteCategoryId id,
        List<string> path)
    {
        foreach (var category in categories)
        {
            path.Add(category.Name);
            if (category.Id == id || TryBuildPath(category.Children, id, path))
            {
                return true;
            }

            path.RemoveAt(path.Count - 1);
        }

        return false;
    }

    private string ParentPathFor(PaletteCategoryId id)
    {
        var path = PathTo(id);
        return path.Count > 1 ? string.Join(" › ", path.Take(path.Count - 1)) + " ›" : string.Empty;
    }

    private PaletteCategoryRow? FindVisibleRow(PaletteCategoryId id) =>
        Rows.FirstOrDefault(row => row.Id == id);

    private async Task LoadPreviewAsync(
        PaletteEntryRow row,
        PaletteEntrySnapshot entry,
        CancellationTokenSource cancellation)
    {
        try
        {
            var preview = await _actions.LoadPreviewAsync(entry, cancellation.Token);
            if (cancellation.IsCancellationRequested
                || row.SnapshotRevision != _snapshot.Revision
                || !_entries.TryGetValue(entry.Id, out var current)
                || current != entry
                || !Tiles.Contains(row))
            {
                return;
            }

            if (preview is null)
            {
                row.PreviewRequested = false;
                _failedPreviewEntries.Add(entry.Id);
                _localStatusMessage = Texts.Get(PaletteStringId.PreviewUnavailable, entry.Name);
                OnPropertyChanged(nameof(StatusMessage));
                return;
            }

            row.Preview = preview;
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            if (!IsExpectedPreviewFailure(exception))
            {
                Trace.TraceError("Unexpected palette preview failure for {0}: {1}", entry.ResRef, exception);
            }

            if (!cancellation.IsCancellationRequested
                && Tiles.Contains(row)
                && _entries.TryGetValue(entry.Id, out var current)
                && current == entry)
            {
                row.PreviewRequested = false;
                _failedPreviewEntries.Add(entry.Id);
                _localStatusMessage = Texts.Get(PaletteStringId.PreviewUnavailable, entry.Name);
                OnPropertyChanged(nameof(StatusMessage));
            }
        }
        finally
        {
            var disposeCancellation = false;
            lock (_previewRequestGate)
            {
                if (_previewRequests.TryGetValue(entry.Id, out var currentCancellation)
                    && ReferenceEquals(currentCancellation, cancellation))
                {
                    _previewRequests.Remove(entry.Id);
                    disposeCancellation = true;
                }
            }

            if (disposeCancellation)
            {
                cancellation.Dispose();
            }
        }
    }

    private static bool IsExpectedPreviewFailure(Exception exception) =>
        exception is IOException or InvalidDataException or FormatException;

    private void CancelAllPreviewRequests()
    {
        PaletteEntryId[] requestIds;
        lock (_previewRequestGate)
        {
            requestIds = _previewRequests.Keys.ToArray();
        }

        foreach (var id in requestIds)
        {
            CancelPreviewRequest(id);
        }
    }

    private void CancelPreviewRequest(PaletteEntryId id)
    {
        CancellationTokenSource? cancellation;
        lock (_previewRequestGate)
        {
            if (!_previewRequests.Remove(id, out cancellation))
            {
                return;
            }
        }

        cancellation.Cancel();
        cancellation.Dispose();
    }

    private void NotifySnapshotPropertiesChanged()
    {
        OnPropertyChanged(nameof(Mode));
        OnPropertyChanged(nameof(SelectedType));
        OnPropertyChanged(nameof(Source));
        OnPropertyChanged(nameof(TilePaintMode));
        OnPropertyChanged(nameof(HasOpenArea));
        OnPropertyChanged(nameof(IsTileMode));
        OnPropertyChanged(nameof(IsBlueprintMode));
        OnPropertyChanged(nameof(ShowsSourceSwitch));
        OnPropertyChanged(nameof(ShowsTilePaintSwitch));
        OnPropertyChanged(nameof(IsCustomSource));
        OnPropertyChanged(nameof(IsStandardSource));
        OnPropertyChanged(nameof(IsAutoTilePaint));
        OnPropertyChanged(nameof(IsManualTilePaint));
        OnPropertyChanged(nameof(NeedsOpenArea));
        OnPropertyChanged(nameof(StatusMessage));
        OnPropertyChanged(nameof(NewBlueprintLabel));
    }

    private void NotifyEntryPropertiesChanged()
    {
        OnPropertyChanged(nameof(CanEditCopy));
        OnPropertyChanged(nameof(HasBlueprintActions));
        OnPropertyChanged(nameof(ReadOnlyNotice));
        OnPropertyChanged(nameof(HasReadOnlyNotice));
        OnPropertyChanged(nameof(StatusMessage));
        OnPropertyChanged(nameof(Breadcrumb));
    }

    private static PaletteSnapshot EmptySnapshot() => new(
        0,
        PaletteMode.Blueprints,
        null,
        PaletteSource.Custom,
        Array.Empty<PaletteTypeOption>(),
        Array.Empty<PaletteCategorySnapshot>(),
        Array.Empty<PaletteEntrySnapshot>(),
        PaletteTilePaintMode.Auto,
        false,
        null,
        new PaletteCapabilities(false, false, false, false));

    private static IEnumerable<PaletteCategorySnapshot> Flatten(
        IReadOnlyList<PaletteCategorySnapshot> categories)
    {
        foreach (var category in categories)
        {
            yield return category;
            foreach (var descendant in Flatten(category.Children))
            {
                yield return descendant;
            }
        }
    }

    private static void ValidateSnapshot(PaletteSnapshot snapshot)
    {
        if (snapshot.Mode == PaletteMode.Tiles && snapshot.SelectedType is not null)
        {
            throw new ArgumentException("A tile palette cannot select a blueprint resource type.", nameof(snapshot));
        }

        if (snapshot.Mode == PaletteMode.Blueprints && snapshot.SelectedType is null)
        {
            throw new ArgumentException("A blueprint palette requires a resource type.", nameof(snapshot));
        }

        var categoryIds = Flatten(snapshot.Categories).Select(category => category.Id).ToArray();
        if (categoryIds.Distinct().Count() != categoryIds.Length)
        {
            throw new ArgumentException("Palette category IDs must be unique within a snapshot.", nameof(snapshot));
        }

        if (snapshot.InitialSelectedCategory is { } initialCategoryId && !categoryIds.Contains(initialCategoryId))
        {
            throw new ArgumentException("The initial selected category must belong to the snapshot.", nameof(snapshot));
        }

        if (snapshot.Entries.Select(entry => entry.Id).Distinct().Count() != snapshot.Entries.Count)
        {
            throw new ArgumentException("Palette entry IDs must be unique within a snapshot.", nameof(snapshot));
        }
    }
}
