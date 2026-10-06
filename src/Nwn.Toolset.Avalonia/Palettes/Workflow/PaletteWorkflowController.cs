using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nwn.Authoring.Areas.Tiles;
using Nwn.Authoring.Categories;
using Nwn.Authoring.Resources;

namespace Nwn.Toolset.Avalonia.Palettes.Workflow;

/// <summary>
/// The palette's workflow: pick a blueprint type or Tiles, browse or search its categories, and place an
/// entry into the open area or open it for editing - plus the category and blueprint commands around that.
/// </summary>
/// <remarks>
/// <para>
/// Hosts supply their data and policies through <see cref="PaletteWorkflowHost"/> and bind the shared
/// palette view to <see cref="PresentationState"/>. Every snapshot is rebuilt from the host's category
/// sections, so the projection (folders, pins, Unsorted, counts) and every command behave the same in
/// every host.
/// </para>
/// <para>
/// Custom is the default source: it is where a builder spends effectively all their time. The Standard
/// side is the base game's content, which is not ours to rename, delete, refile or add to, so every
/// command that would write is hidden there rather than disabled.
/// </para>
/// </remarks>
public sealed partial class PaletteWorkflowController : ObservableObject, IPaletteActions, IDisposable
{
    private readonly PaletteWorkflowHost _host;
    private readonly PaletteCategoryCommands _categoryCommands;
    private readonly PaletteEntryCommands _entryCommands;
    private readonly HashSet<PaletteCategoryId> _presentationCategoryIds = new();
    private readonly Dictionary<PaletteEntryId, TilePaletteEntry> _presentationTileEntries = new();
    private readonly Dictionary<PaletteEntryId, PaletteEntrySnapshot> _presentationEntries = new();
    private readonly bool _constructed;

    /// <summary>
    /// True while saved preferences are being applied, so restoring one neither writes it straight back
    /// nor rebuilds a tree that has not been built yet.
    /// </summary>
    private bool _restoring;

    private bool _disposed;
    private IReadOnlySet<string> _existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private long _presentationRevision;
    private TilePalette _tiles = TilePalette.Empty;

    public PaletteWorkflowController(PaletteWorkflowHost host)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        ArgumentNullException.ThrowIfNull(host.Content);
        ArgumentNullException.ThrowIfNull(host.Categories);

        Texts = host.WorkflowTexts ?? PaletteWorkflowTexts.English;
        PaletteTexts = host.PaletteTexts ?? PaletteTexts.English;
        OfferedTypes = host.Content.OfferedTypes.ToArray();
        _selectedType = OfferedTypes.Contains(ModuleResourceType.Utp) || OfferedTypes.Count == 0
            ? ModuleResourceType.Utp
            : OfferedTypes[0];
        _categoryCommands = new PaletteCategoryCommands(this);
        _entryCommands = new PaletteEntryCommands(this);
        NewBlueprintCommand = new AsyncRelayCommand(_entryCommands.NewBlueprintAsync);
        // Not a cancelable command: starting one would cancel the copy still being written for the
        // previous entry. A host that wants to cancel a copy awaits EditCopyAsync with its own token.
        EditCopyCommand = new AsyncRelayCommand<PaletteEntrySnapshot?>(
            entry => entry is null ? Task.CompletedTask : _entryCommands.EditCopyAsync(entry, CancellationToken.None),
            AsyncRelayCommandOptions.AllowConcurrentExecutions);

        if (host.WriteGate is { } writeGate)
            writeGate.Changed += NotifyWriteAvailabilityChanged;
        if (host.Previews is { } previews)
            previews.Invalidated += OnPreviewInvalidated;

        RestoreSettings();
        TypeOptions = BuildTypeOptions();
        PresentationState = new PalettePresentationState(this, PaletteTexts);
        _constructed = true;

        host.Categories.Changed += Refresh;
    }

    /// <summary>The state the shared palette view binds to.</summary>
    public PalettePresentationState PresentationState { get; }

    /// <summary>The workflow's own text.</summary>
    public PaletteWorkflowTexts Texts { get; }

    /// <summary>The shared palette view's text.</summary>
    public PaletteTexts PaletteTexts { get; }

    /// <summary>The blueprint types offered, in the host's order.</summary>
    public IReadOnlyList<ModuleResourceType> OfferedTypes { get; }

    /// <summary>The type row: Tiles, then every offered type.</summary>
    public IReadOnlyList<PaletteTypeOption> TypeOptions { get; }

    /// <summary>
    /// Creates a blueprint of the active type and files it into the selected category. Cancelable: the
    /// token reaches the host's creation dialog and write, and disposing the palette cancels it.
    /// </summary>
    public IAsyncRelayCommand NewBlueprintCommand { get; }

    /// <summary>
    /// Edit Copy for an entry: the command <see cref="EditCopy"/> runs, so a host can observe the copy it
    /// started. Use <see cref="EditCopyAsync"/> to pass a cancellation token.
    /// </summary>
    public IAsyncRelayCommand<PaletteEntrySnapshot?> EditCopyCommand { get; }

    [ObservableProperty]
    private ModuleResourceType _selectedType;

    /// <summary>Whether the tree and grid show the module's own blueprints or the base game's.</summary>
    [ObservableProperty]
    private PaletteSource _source = PaletteSource.Custom;

    /// <summary>
    /// True when Tiles is picked instead of a blueprint type. Which tiles exist is a property of the open
    /// area's tileset, so this mode reads from the area in front, has no Custom/Standard split, and
    /// cannot create, rename or delete anything.
    /// </summary>
    [ObservableProperty]
    private bool _isTileMode;

    [ObservableProperty]
    private string? _statusMessage;

    /// <summary>Tile width in pixels.</summary>
    [ObservableProperty]
    private double _tileSize = 136;

    /// <summary>
    /// Whether the panel shows its "no area open" state: Tiles is selected and there is no area in front
    /// to take a tileset from. A state rather than a status line, so it never lingers under a list of
    /// blueprints where it is no longer true.
    /// </summary>
    [ObservableProperty]
    private bool _needsOpenArea;

    /// <summary>
    /// Whether a click picks the tile itself or only the terrain and lets the tileset choose. Auto is the
    /// default because it is what Aurora does and what laying a floor means.
    /// </summary>
    [ObservableProperty]
    private TilePaintMode _tilePaintMode = TilePaintMode.Auto;

    /// <summary>
    /// Share of the panel's flexible height the category tree keeps, or 0 when that divider has not
    /// been moved. Stored rather than bound, because the view's grid owns the live value.
    /// </summary>
    public double CategoryProportion
    {
        get => _host.Settings?.CategoryProportion ?? 0;
        set
        {
            if (_host.Settings is { } settings)
                settings.CategoryProportion = value;
        }
    }

    public bool IsCustomSource => Source == PaletteSource.Custom;

    public bool IsStandardSource => !IsCustomSource;

    /// <summary>Everything that writes to the module or the sidecar is blueprint-only.</summary>
    public bool IsBlueprintMode => !IsTileMode;

    /// <summary>Custom/Standard is meaningless for tiles: which tileset is in play is decided by the area.</summary>
    public bool ShowsSourceSwitch => !IsTileMode;

    /// <summary>The Auto/Manual switch replaces Custom/Standard while Tiles is showing.</summary>
    public bool ShowsTilePaintSwitch => IsTileMode;

    public bool IsAutoTilePaint => TilePaintMode == TilePaintMode.Auto;

    public bool IsManualTilePaint => TilePaintMode == TilePaintMode.Manual;

    /// <summary>
    /// True when this palette may write to the module: the Custom side of a blueprint type, and no
    /// module-wide operation in flight. A pack reads the very files a create or delete writes.
    /// </summary>
    public bool CanWrite => IsCustomSource && IsBlueprintMode && _host.WriteGate?.IsLocked != true;

    /// <summary>
    /// Edit Copy writes a new module blueprint but never changes its source, so it is available on both
    /// sides whenever ordinary module writes are available.
    /// </summary>
    public bool CanEditCopy => IsBlueprintMode && _host.WriteGate?.IsLocked != true;

    /// <summary>
    /// Creation is narrower than editing: types whose editor cannot finish a usable resource stay
    /// browsable and editable but do not offer a misleading "New" action.
    /// </summary>
    public bool CanCreateBlueprint => CanWrite && _host.Blueprints?.CanCreate(SelectedType) == true;

    /// <summary>Whether a blueprint tile has anything useful to expose through its menu.</summary>
    public bool HasBlueprintActions => CanWrite || CanEditCopy || HasReadOnlyNotice;

    /// <summary>
    /// Why a context menu is empty, so it never opens as a blank popup. Null exactly when the menu has
    /// real items on it.
    /// </summary>
    public string? ReadOnlyNotice =>
        IsTileMode ? Texts.Get(PaletteWorkflowStringId.TilesetReadOnly)
        : IsStandardSource ? Texts.Get(PaletteWorkflowStringId.BaseGameReadOnly)
        : null;

    public bool HasReadOnlyNotice => ReadOnlyNotice != null;

    /// <summary>The label for the type-specific create action, e.g. "New Placeable...".</summary>
    public string NewBlueprintLabel =>
        Texts.Get(PaletteWorkflowStringId.NewTypedBlueprint, _host.Content.SingularName(SelectedType));

    internal PaletteWorkflowHost Host => _host;

    /// <summary>
    /// The category the next category or blueprint command acts on, resolved by path against the tree the
    /// host holds now. Never a remembered folder object: a host replaces its tree when it restores the
    /// persisted copy after a refused save, reloads the sidecar or repairs placeholder names, and a
    /// remembered folder would then be one the current tree does not contain.
    /// </summary>
    internal CategoryFolder? SelectedFolder =>
        !IsTileMode && SelectedFolderPathKey is { } pathKey ? CurrentSection()?.FindByPathKey(pathKey) : null;

    /// <summary>The selected folder's path key, or null when Unsorted, a tile category or nothing is selected.</summary>
    internal string? SelectedFolderPathKey =>
        SelectedCategoryId is { } id && TryGetFolderPathKey(id, out var pathKey) ? pathKey : null;

    /// <summary>
    /// The selected category's identity - a folder's path, Unsorted, or a tile category - for the type and
    /// source on screen.
    /// </summary>
    internal PaletteCategoryId? SelectedCategoryId { get; set; }

    /// <summary>A stable key for a type in identities and log lines, e.g. "utp".</summary>
    internal string TypeKey => SelectedType.ToString().ToLowerInvariant();

    /// <summary>What every folder identity of the type and source on screen starts with; its path follows.</summary>
    private string FolderCategoryPrefix => $"{TypeKey}/{Source}/folder/";

    /// <summary>Re-reads the write capabilities, for when the module-wide lock has flipped.</summary>
    public void NotifyWriteAvailabilityChanged()
    {
        OnPropertyChanged(nameof(CanWrite));
        OnPropertyChanged(nameof(CanEditCopy));
        OnPropertyChanged(nameof(CanCreateBlueprint));
        OnPropertyChanged(nameof(HasBlueprintActions));
        PublishPresentationSnapshot();
    }

    /// <summary>Rebuilds the tree and grid for the current type. Safe to call whenever the module changes.</summary>
    public void Refresh()
    {
        if (IsTileMode)
        {
            RefreshTiles();
            return;
        }

        NeedsOpenArea = false;

        _existing = IsCustomSource
            ? _host.Content.CustomResRefs(SelectedType)
            : _host.Content.Standard(SelectedType).ResRefs;
        PublishPresentationSnapshot();
    }

    /// <summary>
    /// The area in front changed. Only Tiles mode cares: blueprints are the module's, the same whichever
    /// tab has focus, while a tileset belongs to one area.
    /// </summary>
    public void OnActiveAreaChanged()
    {
        if (IsTileMode)
            RefreshTiles();
    }

    public void SelectType(PaletteTypeOption type)
    {
        ArgumentNullException.ThrowIfNull(type);
        if (type.IsTiles)
        {
            if (IsTileMode)
                return;

            IsTileMode = true;
            return;
        }

        var selected = type.Type!.Value;
        if (!OfferedTypes.Contains(selected))
            return;

        if (!IsTileMode && selected == SelectedType)
            return;

        IsTileMode = false;
        SelectedType = selected;
    }

    public void SelectSource(PaletteSource source) => Source = source;

    public void SelectMode(PaletteMode mode) => IsTileMode = mode == PaletteMode.Tiles;

    public void SelectTilePaintMode(PaletteTilePaintMode mode) =>
        TilePaintMode = mode == PaletteTilePaintMode.Auto ? TilePaintMode.Auto : TilePaintMode.Manual;

    public void SelectCategory(PaletteCategoryId? categoryId) =>
        SelectedCategoryId = categoryId is { } id && _presentationCategoryIds.Contains(id) ? id : null;

    public void SetTileSize(double tileSize) => TileSize = tileSize;

    public void SetCategoryProportion(double categoryProportion) => CategoryProportion = categoryProportion;

    public void Place(PaletteEntrySnapshot entry) => _entryCommands.Place(entry);

    public void Edit(PaletteEntrySnapshot entry) => _entryCommands.Edit(entry);

    public void EditCopy(PaletteEntrySnapshot entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        EditCopyCommand.Execute(entry);
    }

    /// <summary>
    /// Copies an entry and, once the host has written the copy, files, reveals and opens it. What
    /// <see cref="EditCopy"/> starts, for a host that wants to await it.
    /// </summary>
    public Task EditCopyAsync(PaletteEntrySnapshot entry, CancellationToken cancellationToken) =>
        _entryCommands.EditCopyAsync(entry, cancellationToken);

    public Task DeleteAsync(PaletteEntrySnapshot entry, CancellationToken cancellationToken) =>
        _entryCommands.DeleteAsync(entry, cancellationToken);

    public void NewBlueprint(PaletteCategoryId? categoryId)
    {
        SelectCategory(categoryId);
        NewBlueprintCommand.Execute(null);
    }

    public async Task NewCategoryAsync(PaletteCategoryId? categoryId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SelectCategory(categoryId);
        await _categoryCommands.NewCategoryAsync().ConfigureAwait(true);
    }

    public async Task RenameCategoryAsync(PaletteCategoryId categoryId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SelectCategory(categoryId);
        await _categoryCommands.RenameCategoryAsync().ConfigureAwait(true);
    }

    public async Task DeleteCategoryAsync(PaletteCategoryId categoryId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SelectCategory(categoryId);
        await _categoryCommands.DeleteCategoryAsync().ConfigureAwait(true);
    }

    public Task TogglePinAsync(PaletteCategoryId categoryId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SelectCategory(categoryId);
        _categoryCommands.TogglePin();
        return Task.CompletedTask;
    }

    public void FileSelectedEntry(PaletteCategoryId categoryId)
    {
        SelectCategory(categoryId);
        _categoryCommands.FileSelectedEntry();
    }

    public ValueTask<Bitmap?> LoadPreviewAsync(PaletteEntrySnapshot entry, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (_host.Previews is not { IsAvailable: true } previews)
            return ValueTask.FromResult<Bitmap?>(null);

        if (cancellationToken.IsCancellationRequested)
            return ValueTask.FromCanceled<Bitmap?>(cancellationToken);

        // The token reaches the host so a preview nobody wants any more stops rendering; WaitAsync still
        // releases the caller at once for a host that cannot stop mid-render.
        Task<Bitmap?> request;
        if (entry.Kind == PaletteEntryKind.Tile && _presentationTileEntries.TryGetValue(entry.Id, out var tile))
            request = previews.LoadTileAsync(tile, cancellationToken);
        else if (entry.ResourceType is { } resourceType)
            request = previews.LoadBlueprintAsync(resourceType, entry.ResRef, entry.Source, cancellationToken);
        else
            return ValueTask.FromResult<Bitmap?>(null);

        return new ValueTask<Bitmap?>(request.WaitAsync(cancellationToken));
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        NewBlueprintCommand.Cancel();
        _host.Categories.Changed -= Refresh;
        if (_host.WriteGate is { } writeGate)
            writeGate.Changed -= NotifyWriteAvailabilityChanged;
        if (_host.Previews is { } previews)
            previews.Invalidated -= OnPreviewInvalidated;
        PresentationState.Dispose();
    }

    /// <summary>
    /// The category tree currently in play. Null only when the module has no section for this type; the
    /// Standard side always returns a section, empty when the base game is unavailable.
    /// </summary>
    internal CategorySection? CurrentSection() =>
        IsCustomSource ? _host.Categories.Section(SelectedType) : _host.Content.Standard(SelectedType).Section;

    /// <summary>
    /// Whether a retained snapshot entry still belongs to what the palette shows now. A menu opened before
    /// the type or source changed must not act on the old one.
    /// </summary>
    internal bool TryGetCurrentEntry(PaletteEntrySnapshot entry, out PaletteEntrySnapshot current)
    {
        if (!_presentationEntries.TryGetValue(entry.Id, out current!) ||
            !ReferenceEquals(current, entry))
        {
            return false;
        }

        if (current.Kind == PaletteEntryKind.Tile)
            return IsTileMode && current.ResourceType == null && current.Source == PaletteSource.Custom;

        return !IsTileMode && current.ResourceType == SelectedType && current.Source == Source;
    }

    internal bool TryGetTile(PaletteEntryId id, out TilePaletteEntry tile) =>
        _presentationTileEntries.TryGetValue(id, out tile!);

    internal PaletteCategoryId FolderCategoryId(CategorySection section, CategoryFolder folder) =>
        FolderCategoryId(section.PathKey(folder));

    internal PaletteCategoryId FolderCategoryId(string pathKey) => new(FolderCategoryPrefix + pathKey);

    /// <summary>The folder path a category identity names, for the type and source on screen.</summary>
    internal bool TryGetFolderPathKey(PaletteCategoryId id, out string pathKey)
    {
        var prefix = FolderCategoryPrefix;
        if (id.Value is { } value && value.Length > prefix.Length &&
            value.StartsWith(prefix, StringComparison.Ordinal))
        {
            pathKey = value[prefix.Length..];
            return true;
        }

        pathKey = string.Empty;
        return false;
    }

    internal PaletteCategoryId UnsortedCategoryId() => new($"{TypeKey}/{Source}/unsorted");

    internal PaletteEntryId BlueprintEntryId(string resRef) =>
        new($"{TypeKey}/{Source}/{resRef.ToLowerInvariant()}");

    internal void Log(string message) => _host.Log?.Write(message);

    /// <summary>
    /// Writes the category sidecar and reports a refusal in the status line. The sidecar can legitimately
    /// decline - it is read-only when a newer toolset wrote it, and it will not clobber an external edit -
    /// and every command has already told the builder what it did, so a silent refusal would mislead.
    /// </summary>
    internal bool SaveCategories()
    {
        var result = _host.Categories.SaveChanges();
        if (!result.Saved)
            StatusMessage = result.Problem;

        return result.Saved;
    }

    internal void PublishPresentationSnapshot()
    {
        _presentationCategoryIds.Clear();
        _presentationTileEntries.Clear();
        _presentationEntries.Clear();

        var categories = new List<PaletteCategorySnapshot>();
        var entries = new List<PaletteEntrySnapshot>();
        PaletteCategoryId? initialSelectedCategory = null;

        if (IsTileMode)
        {
            initialSelectedCategory = ProjectTiles(categories, entries);
        }
        else if (CurrentSection() is { } section)
        {
            initialSelectedCategory = ProjectBlueprints(section, categories, entries);
        }

        var snapshot = new PaletteSnapshot(
            Interlocked.Increment(ref _presentationRevision),
            IsTileMode ? PaletteMode.Tiles : PaletteMode.Blueprints,
            IsTileMode ? null : SelectedType,
            Source,
            TypeOptions,
            categories,
            entries,
            TilePaintMode == TilePaintMode.Auto ? PaletteTilePaintMode.Auto : PaletteTilePaintMode.Manual,
            !NeedsOpenArea,
            string.IsNullOrWhiteSpace(StatusMessage) ? null : StatusMessage,
            new PaletteCapabilities(true, true, true, true),
            TileSize,
            CategoryProportion,
            initialSelectedCategory);
        PresentationState.SetSnapshot(snapshot);
    }

    private PaletteCategoryId? ProjectTiles(List<PaletteCategorySnapshot> categories, List<PaletteEntrySnapshot> entries)
    {
        var offered = TilePaintModes.CategoriesFor(_tiles, TilePaintMode);
        var readOnly = Texts.Get(PaletteWorkflowStringId.TilesetReadOnly);
        var categoryKeys = TileCategoryKeys(offered);

        for (var categoryIndex = 0; categoryIndex < offered.Count; categoryIndex++)
        {
            var category = offered[categoryIndex];
            var categoryKey = categoryKeys[categoryIndex];
            var categoryId = new PaletteCategoryId($"tiles/{categoryKey}");
            var tileIds = new List<PaletteEntryId>();
            _presentationCategoryIds.Add(categoryId);

            for (var entryIndex = 0; entryIndex < category.Entries.Count; entryIndex++)
            {
                var tile = category.Entries[entryIndex];
                var entryId = new PaletteEntryId($"tile/{categoryKey}/{entryIndex}/{tile.PreviewModelResRef}");
                tileIds.Add(entryId);
                _presentationTileEntries[entryId] = tile;
                var tileSnapshot = new PaletteEntrySnapshot(
                    entryId,
                    PaletteEntryKind.Tile,
                    null,
                    PaletteSource.Custom,
                    tile.PreviewModelResRef,
                    tile.Label,
                    string.Empty,
                    new[] { categoryId },
                    tile.Columns,
                    tile.Rows,
                    tile.FootprintModelResRefs ?? Array.Empty<string>(),
                    new PaletteEntryCapabilities(!NeedsOpenArea, false, false, false, readOnly),
                    SupportsPreview: tile.Crosser is not { Length: 0 });
                _presentationEntries[entryId] = tileSnapshot;
                entries.Add(tileSnapshot);
            }

            categories.Add(new PaletteCategorySnapshot(
                categoryId,
                category.Name,
                category.Entries.Count,
                false,
                categoryIndex,
                Array.Empty<PaletteCategorySnapshot>(),
                tileIds,
                new PaletteCategoryCapabilities(false, false, false, false, false, false, readOnly)));
        }

        // A category both paint modes offer (Features, Groups) stays selected across a switch, and one of
        // the same name stays selected across tilesets; anything else starts at the first category.
        SelectedCategoryId = SelectedCategoryId is { } selected && _presentationCategoryIds.Contains(selected)
            ? selected
            : categories.FirstOrDefault()?.Id;
        return SelectedCategoryId;
    }

    /// <summary>
    /// Stable identities for tile categories: the category's name, which is what a builder recognises and
    /// what survives a paint-mode switch. A name repeated within one palette is told apart by occurrence
    /// ("Name#2"), deterministically, rather than by list position, which shifts between modes.
    /// </summary>
    private static string[] TileCategoryKeys(IReadOnlyList<TilePaletteCategory> categories)
    {
        var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var keys = new string[categories.Count];
        for (var index = 0; index < categories.Count; index++)
        {
            var name = categories[index].Name;
            var occurrence = seen.TryGetValue(name, out var count) ? count + 1 : 1;
            seen[name] = occurrence;
            keys[index] = occurrence == 1 ? name : $"{name}#{occurrence}";
        }

        return keys;
    }

    private PaletteCategoryId? ProjectBlueprints(
        CategorySection section,
        List<PaletteCategorySnapshot> categories,
        List<PaletteEntrySnapshot> entries)
    {
        foreach (var folder in section.Folders)
            categories.Add(BuildFolderSnapshot(folder, section));

        var unsortedId = UnsortedCategoryId();
        var unsortedEntryIds = section.UnsortedResRefs(_existing)
            .Select(BlueprintEntryId)
            .ToArray();
        categories.Add(new PaletteCategorySnapshot(
            unsortedId,
            Texts.Get(PaletteWorkflowStringId.Unsorted),
            unsortedEntryIds.Length,
            false,
            int.MaxValue,
            Array.Empty<PaletteCategorySnapshot>(),
            unsortedEntryIds,
            CreateCategoryCapabilities(isFolder: false)));
        _presentationCategoryIds.Add(unsortedId);

        var names = new PaletteEntryNames(_host.Content, SelectedType, Source);
        var canPlace = _host.Placement?.ActiveTarget is not null;
        foreach (var resRef in _existing)
        {
            var entry = CreateBlueprintEntry(resRef, section, names, canPlace);
            entries.Add(entry);
            _presentationEntries[entry.Id] = entry;
        }

        // The selection is a path, so a host that replaced its tree since (a refused save restoring the
        // persisted copy, a reload) still finds the same folder. One the current tree no longer holds is
        // dropped rather than projected: a snapshot may only select a category it contains.
        if (SelectedCategoryId is { } selectedCategoryId)
        {
            if (_presentationCategoryIds.Contains(selectedCategoryId))
                return selectedCategoryId;

            SelectedCategoryId = null;
        }

        return null;
    }

    private PaletteCategorySnapshot BuildFolderSnapshot(CategoryFolder folder, CategorySection section)
    {
        var categoryId = FolderCategoryId(section, folder);
        var memberIds = folder.Members
            .Where(_existing.Contains)
            .Select(BlueprintEntryId)
            .ToArray();
        var children = folder.Children
            .Select(child => BuildFolderSnapshot(child, section))
            .ToArray();
        var pathKey = section.PathKey(folder);
        var pinOrder = section.Pinned.ToList().FindIndex(
            pin => string.Equals(pin, pathKey, StringComparison.OrdinalIgnoreCase));
        var isPinned = pinOrder >= 0;

        _presentationCategoryIds.Add(categoryId);
        return new PaletteCategorySnapshot(
            categoryId,
            folder.Name,
            section.CountIn(folder, _existing),
            isPinned,
            isPinned ? pinOrder : int.MaxValue,
            children,
            memberIds,
            CreateCategoryCapabilities(isFolder: true));
    }

    private PaletteCategoryCapabilities CreateCategoryCapabilities(bool isFolder) =>
        new(
            CanCreateBlueprint,
            CanWrite,
            isFolder && CanWrite,
            isFolder && CanWrite,
            isFolder && CanWrite,
            isFolder && CanWrite,
            ReadOnlyNotice);

    private PaletteEntrySnapshot CreateBlueprintEntry(
        string resRef,
        CategorySection section,
        PaletteEntryNames names,
        bool canPlace)
    {
        var categoryIds = section.FoldersContaining(resRef)
            .Select(folder => FolderCategoryId(section, folder))
            .ToArray();
        if (categoryIds.Length == 0)
            categoryIds = new[] { UnsortedCategoryId() };

        return new PaletteEntrySnapshot(
            BlueprintEntryId(resRef),
            PaletteEntryKind.Blueprint,
            SelectedType,
            Source,
            resRef,
            names.NameFor(resRef),
            resRef,
            categoryIds,
            null,
            null,
            Array.Empty<string>(),
            new PaletteEntryCapabilities(
                canPlace,
                CanWrite,
                CanEditCopy,
                CanWrite && _host.Prompts is not null && _host.Blueprints?.CanDelete(SelectedType) == true,
                IsStandardSource ? ReadOnlyNotice : null));
    }

    /// <summary>
    /// Rebuilds the tile tree from the tileset of whatever area is in front. Re-read on every refresh
    /// because two areas on different tilesets offer different tiles; the host caches the parse.
    /// </summary>
    private void RefreshTiles()
    {
        _presentationCategoryIds.Clear();
        _presentationTileEntries.Clear();
        _tiles = TilePalette.Empty;

        var tilesetResRef = _host.Placement?.ActiveTarget?.TilesetResRef;
        NeedsOpenArea = string.IsNullOrWhiteSpace(tilesetResRef);
        if (NeedsOpenArea)
        {
            StatusMessage = string.Empty;
            PublishPresentationSnapshot();
            return;
        }

        if (_host.Tilesets?.Load(tilesetResRef!) is not { } tileset)
        {
            StatusMessage = PaletteTexts.Get(PaletteStringId.TilesetNotLoaded, tilesetResRef);
            PublishPresentationSnapshot();
            return;
        }

        _tiles = tileset.Palette;
        if (_tiles.IsEmpty)
        {
            StatusMessage = PaletteTexts.Get(PaletteStringId.TilesetHasNoTiles, tilesetResRef);
            PublishPresentationSnapshot();
            return;
        }

        var offered = TilePaintModes.CategoriesFor(_tiles, TilePaintMode);
        if (offered.Count == 0)
        {
            StatusMessage = IsAutoTilePaint
                ? PaletteTexts.Get(PaletteStringId.SelectTerrainToPaint, tileset.DisplayName)
                : PaletteTexts.Get(PaletteStringId.SelectTileToStamp, tileset.DisplayName);
            PublishPresentationSnapshot();
            return;
        }

        StatusMessage = IsAutoTilePaint
            ? Texts.Get(PaletteWorkflowStringId.TerrainReady, tileset.DisplayName)
            : Texts.Get(PaletteWorkflowStringId.TileReady, tileset.DisplayName);
        PublishPresentationSnapshot();
    }

    /// <summary>
    /// Applies what the builder left set last time: preview size, which type was showing, which side,
    /// and the tile paint mode. Runs before any tree exists, so it only assigns state.
    /// </summary>
    private void RestoreSettings()
    {
        if (_host.Settings is not { } settings)
            return;

        _restoring = true;
        try
        {
            if (settings.PreviewSize > 0)
                TileSize = settings.PreviewSize;

            // Three outcomes, not two: nothing saved leaves the default type alone, Tiles restores Tiles
            // mode, and anything else is a blueprint type. Collapsing the first two is how a fresh
            // install once opened in Tiles mode.
            var selection = settings.Selection;
            if (selection is { Mode: PaletteMode.Tiles })
                IsTileMode = true;
            else if (selection is { Type: { } type } && OfferedTypes.Contains(type))
                SelectedType = type;

            Source = settings.Source;

            if (settings.TilePaintMode is { } paintMode)
                TilePaintMode = paintMode;
        }
        finally
        {
            _restoring = false;
        }
    }

    /// <summary>
    /// Every type, always: as icons they all fit one row of a narrow panel. Tiles leads, as in Aurora - it
    /// is what you reach for while the area is still a grid of nothing.
    /// </summary>
    private PaletteTypeOption[] BuildTypeOptions()
    {
        var tilesLabel = Texts.Get(PaletteWorkflowStringId.TilesType);
        var options = new List<PaletteTypeOption>
        {
            new(null, tilesLabel, Initial(tilesLabel), Texts.Get(PaletteWorkflowStringId.NewUntypedBlueprint),
                _host.Previews?.TypeIcon(null))
        };

        foreach (var type in OfferedTypes)
        {
            var label = _host.Content.PluralName(type);
            options.Add(new PaletteTypeOption(
                type,
                label,
                Initial(label),
                Texts.Get(PaletteWorkflowStringId.NewTypedBlueprint, _host.Content.SingularName(type)),
                _host.Previews?.TypeIcon(type)));
        }

        return options.ToArray();
    }

    private string Initial(string label) =>
        label.Length > 0 ? label[..1] : Texts.Get(PaletteWorkflowStringId.UnknownInitial);

    private void OnPreviewInvalidated(ModuleResourceType type, string resRef)
    {
        if (IsTileMode || type != SelectedType)
            return;

        PresentationState.InvalidatePreview(BlueprintEntryId(resRef));
    }

    partial void OnStatusMessageChanged(string? value)
    {
        if (_constructed)
            PublishPresentationSnapshot();
    }

    partial void OnTileSizeChanged(double value)
    {
        if (_host.Settings is { } settings && !_restoring)
            settings.PreviewSize = value;
    }

    partial void OnSourceChanged(PaletteSource value)
    {
        OnPropertyChanged(nameof(IsCustomSource));
        OnPropertyChanged(nameof(IsStandardSource));
        OnPropertyChanged(nameof(CanWrite));
        OnPropertyChanged(nameof(CanEditCopy));
        OnPropertyChanged(nameof(CanCreateBlueprint));
        OnPropertyChanged(nameof(ReadOnlyNotice));
        OnPropertyChanged(nameof(HasReadOnlyNotice));
        OnPropertyChanged(nameof(HasBlueprintActions));

        if (_restoring)
            return;

        if (_host.Settings is { } settings)
            settings.Source = value;

        SelectedCategoryId = null;
        Refresh();
    }

    partial void OnSelectedTypeChanged(ModuleResourceType value)
    {
        if (_restoring)
            return;

        SelectedCategoryId = null;

        if (_host.Settings is { } settings && !IsTileMode)
            settings.Selection = PaletteSelection.ForType(value);

        OnPropertyChanged(nameof(NewBlueprintLabel));
        OnPropertyChanged(nameof(CanCreateBlueprint));
        Refresh();
    }

    partial void OnIsTileModeChanged(bool value)
    {
        if (_restoring)
            return;

        if (_host.Settings is { } settings)
            settings.Selection = value ? PaletteSelection.Tiles : PaletteSelection.ForType(SelectedType);

        OnPropertyChanged(nameof(IsBlueprintMode));
        OnPropertyChanged(nameof(ShowsSourceSwitch));
        OnPropertyChanged(nameof(ShowsTilePaintSwitch));
        OnPropertyChanged(nameof(CanWrite));
        OnPropertyChanged(nameof(CanEditCopy));
        OnPropertyChanged(nameof(CanCreateBlueprint));
        OnPropertyChanged(nameof(ReadOnlyNotice));
        OnPropertyChanged(nameof(HasReadOnlyNotice));
        OnPropertyChanged(nameof(HasBlueprintActions));
        SelectedCategoryId = null;
        // Tile statuses describe the tileset; a blueprint palette has no counterpart to replace them.
        if (!value)
            StatusMessage = string.Empty;
        Refresh();
    }

    partial void OnTilePaintModeChanged(TilePaintMode value)
    {
        OnPropertyChanged(nameof(IsAutoTilePaint));
        OnPropertyChanged(nameof(IsManualTilePaint));

        if (_restoring)
            return;

        if (_host.Settings is { } settings)
            settings.TilePaintMode = value;

        if (IsTileMode)
            RefreshTiles();
    }
}
