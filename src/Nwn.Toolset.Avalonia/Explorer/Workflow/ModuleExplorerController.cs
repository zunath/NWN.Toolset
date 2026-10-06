using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nwn.Authoring.Categories;
using Nwn.Authoring.Resources;
using Nwn.Toolset.Avalonia.Areas;

namespace Nwn.Toolset.Avalonia.Explorer.Workflow;

/// <summary>
/// Module Contents: the module's own non-blueprint resources (areas, scripts and whatever else the host
/// lists), one tab per section, each organized into folders from the category sidecar.
/// </summary>
/// <remarks>
/// <para>
/// Tabs rather than expandable roots: only one section is ever being worked in, and as roots they cost a
/// row of height each plus a level of indentation on every row beneath. Folders come from the category
/// sidecar - the same store the palette's categories live in - so an arrangement survives a restart
/// without anything being written into the module. A never-organized section is seeded once from the
/// host's naming rules, which turns a fixed automatic grouping into a starting point that can be edited.
/// </para>
/// <para>
/// Rows are published as one flat, virtualized list of every node whose ancestors are expanded. Hosts
/// supply their data and policies through <see cref="ModuleExplorerHost"/>; the projection, search,
/// folder commands, move undo/redo, drag-drop decisions, creation filing and delete orchestration are
/// the same in every host.
/// </para>
/// </remarks>
public sealed partial class ModuleExplorerController : ObservableObject, IDisposable
{
    private readonly ModuleExplorerHost _host;
    private readonly List<ExplorerNodeViewModel> _roots = new();
    private readonly CategoryMembershipHistory _history = new();
    private readonly ExplorerContentSearchCoordinator _search;

    /// <summary>Types whose sidecar section has already been considered for seeding this session.</summary>
    private readonly HashSet<ModuleResourceType> _seeded = new();

    private bool _disposed;

    public ModuleExplorerController(ModuleExplorerHost host)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        ArgumentNullException.ThrowIfNull(host.Content);
        ArgumentNullException.ThrowIfNull(host.Categories);

        Texts = host.Texts ?? ModuleExplorerTexts.English;
        Sections = host.Content.Sections.ToArray();
        _search = new ExplorerContentSearchCoordinator(
            host.Search,
            searching => IsSearchingContent = searching,
            Refresh,
            message => StatusMessage = message);

        // Assigned rather than set through the property: the tab change handler rebuilds a tree that does
        // not exist yet, and the module is not open at construction time anyway.
        _selectedType = Sections.Count > 0 ? Sections[0].Type : default;
        if (host.Settings?.SelectedSection is { } saved && Sections.Any(section => section.Type == saved))
            _selectedType = saved;

        foreach (var section in Sections)
            Tabs.Add(new ExplorerTabViewModel(section.Type, section.Label) { IsSelected = section.Type == _selectedType });

        _history.Changed += NotifyMoveHistoryChanged;
        if (host.WriteGate is { } writeGate)
            writeGate.Changed += OnWriteGateChanged;
        host.Content.ResourceChanged += OnResourceChanged;
        host.Content.ContentChanged += Refresh;
    }

    /// <summary>The panel's text.</summary>
    public ModuleExplorerTexts Texts { get; }

    /// <summary>The host's sections, in tab order.</summary>
    public IReadOnlyList<ExplorerSection> Sections { get; }

    /// <summary>The visible rows: every node whose ancestors are all expanded.</summary>
    public ObservableCollection<ExplorerNodeViewModel> Rows { get; } = new();

    /// <summary>One tab per section.</summary>
    public ObservableCollection<ExplorerTabViewModel> Tabs { get; } = new();

    /// <summary>Every folder of the current tab, as "Move to" destinations.</summary>
    public ObservableCollection<ExplorerMoveTarget> MoveTargets { get; } = new();

    [ObservableProperty]
    private ExplorerNodeViewModel? _selectedRow;

    [ObservableProperty]
    private string _filter = string.Empty;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _isDeletingResource;

    /// <summary>The host's creation form while it is open, or null - the view shows it as an overlay.</summary>
    [ObservableProperty]
    private IAreaCreationFormState? _activeCreationForm;

    /// <summary>Which tab is showing. Everything else in the panel is scoped to it.</summary>
    [ObservableProperty]
    private ModuleResourceType _selectedType;

    /// <summary>True while a content scan is running, so the panel can say so.</summary>
    [ObservableProperty]
    private bool _isSearchingContent;

    /// <summary>The section descriptor of the selected tab.</summary>
    public ExplorerSection SelectedSection =>
        Sections.FirstOrDefault(section => section.Type == SelectedType)
        ?? new ExplorerSection(SelectedType, SelectedType.ToString(), SelectedType.ToString());

    /// <summary>What the New button says, which follows the tab - "New Area...", "New Script...".</summary>
    public string NewItemLabel => Texts.Get(ModuleExplorerStringId.NewItem, SelectedSection.SingularLabel);

    /// <summary>What the panel says while a content scan runs on this tab.</summary>
    public string ContentSearchLabel => _search.SearchingLabel(SelectedType);

    /// <summary>
    /// Whether the selected tab can create. The write gate is the second condition because creating writes
    /// into the folders a pack is copying - an area writes its files and then the module's area list, and a
    /// pack between those two writes captures one without the other.
    /// </summary>
    public bool CanCreateSelectedType =>
        !IsDeletingResource &&
        _host.WriteGate?.IsLocked != true &&
        _host.Creation?.CanCreate(SelectedType) == true;

    /// <summary>Whether the selected tab's resources can be opened.</summary>
    public bool CanOpenSelectedType => !IsDeletingResource && _host.Editors?.CanOpen(SelectedType) == true;

    /// <summary>
    /// Only a compilable section offers Compile - useful after editing an include, when the dependents
    /// needing a rebuild are not the file that was open.
    /// </summary>
    public bool CanCompileSelectedType =>
        !IsDeletingResource && SelectedSection.IsCompilable && _host.WriteGate?.IsLocked != true;

    /// <summary>Only real resource rows can be deleted, and never while a module-wide operation runs.</summary>
    public bool CanDeleteSelectedResource =>
        !IsDeletingResource &&
        SelectedRow?.Item != null &&
        _host.Deletion?.CanDelete(SelectedRow.Type) == true &&
        _host.WriteGate?.IsLocked != true;

    /// <summary>True while a real folder is selected, which is what rename and delete need.</summary>
    public bool HasFolderSelected => SelectedRow?.Folder != null;

    public bool HasMoveTargets => MoveTargets.Count > 0;

    /// <summary>Builds the tree for the selected tab, forgetting move history from a previous module.</summary>
    public void Initialize()
    {
        _history.Clear();
        Refresh();
    }

    /// <summary>
    /// Rebuilds the tree, keeping which folders were open. Expansion is restored by folder name rather
    /// than by node, because every node is new after a rebuild.
    /// </summary>
    public void Refresh()
    {
        var expanded = _roots
            .SelectMany(Flatten)
            .Where(node => node.IsExpanded)
            .Select(node => node.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var selectedResRef = SelectedRow?.Item?.ResRef;

        _roots.Clear();
        Rows.Clear();

        foreach (var tab in Tabs)
            tab.Count = _host.Content.Count(tab.Type);

        var section = _host.Categories.Section(SelectedType);
        if (section == null)
        {
            PublishVisibleRows();
            return;
        }

        var items = _host.Content.Items(SelectedType);
        SeedIfNeeded(section, items);

        var byResRef = Filtered(items)
            .GroupBy(item => item.ResRef, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var builder = new ExplorerTreeBuilder(
            SelectedType, section, _host.Organization, Texts.Get(ModuleExplorerStringId.Unsorted));
        _roots.AddRange(builder.Roots(byResRef));

        foreach (var node in _roots.SelectMany(Flatten))
            node.IsExpanded = expanded.Contains(node.Name);

        PublishMoveTargets(section);
        PublishVisibleRows();

        if (selectedResRef != null)
        {
            SelectedRow = Rows.FirstOrDefault(row =>
                string.Equals(row.Item?.ResRef, selectedResRef, StringComparison.OrdinalIgnoreCase));
        }
    }

    // ----- tabs -----

    [RelayCommand]
    private void SelectTab(ExplorerTabViewModel? tab)
    {
        if (tab == null || tab.Type == SelectedType)
            return;

        SelectedType = tab.Type;
    }

    partial void OnSelectedTypeChanged(ModuleResourceType value)
    {
        if (_host.Settings is { } settings)
            settings.SelectedSection = value;

        foreach (var tab in Tabs)
            tab.IsSelected = tab.Type == value;

        OnPropertyChanged(nameof(SelectedSection));
        OnPropertyChanged(nameof(NewItemLabel));
        OnPropertyChanged(nameof(ContentSearchLabel));
        OnPropertyChanged(nameof(CanOpenSelectedType));
        OnPropertyChanged(nameof(CanCompileSelectedType));
        OnPropertyChanged(nameof(CanCreateSelectedType));
        NewItemCommand.NotifyCanExecuteChanged();
        OpenSelectedCommand.NotifyCanExecuteChanged();
        CompileSelectedCommand.NotifyCanExecuteChanged();
        SelectedRow = null;
        StatusMessage = null;

        // A content scan only means anything on a searchable tab; leaving one running against another tab
        // spends a full corpus read on a result nothing will read.
        _search.Queue(SelectedType, Filter);
        Refresh();
    }

    // ----- creating -----

    /// <summary>Creates a resource of the selected type: the host's form, or a name prompt plus a template.</summary>
    [RelayCommand(CanExecute = nameof(CanCreateSelectedType))]
    private async Task NewItemAsync()
    {
        if (!CanCreateSelectedType)
        {
            StatusMessage = Texts.Get(ModuleExplorerStringId.CreateUnavailableWhileLocked);
            return;
        }

        if (_host.Creation is not { } creation || !_host.Content.IsModuleOpen)
            return;

        var type = SelectedType;
        var section = SelectedSection;

        // Captured now, not read when creation completes: a form is nonmodal, so the builder can switch
        // tabs or select a different folder while it sits open, and the resource must still file into the
        // folder that was current when "New ..." was clicked.
        var targetFolder = SelectedRow?.Folder;

        if (creation.Mode(type) == ModuleExplorerCreationMode.Form)
        {
            OpenCreationForm(creation, type, targetFolder);
            return;
        }

        if (_host.Prompts is not { } prompts)
            return;

        var name = await prompts.PromptForTextAsync(
            Texts.Get(ModuleExplorerStringId.NewItemHeadline, section.SingularLabel),
            Texts.Get(ModuleExplorerStringId.NewItemMessage, section.SingularLabel.ToLowerInvariant()),
            string.Empty,
            Texts.Get(ModuleExplorerStringId.CreateConfirm)).ConfigureAwait(true);

        if (string.IsNullOrWhiteSpace(name))
            return;

        var resRef = creation.ToResRef(type, name);
        if (resRef.Length == 0)
        {
            StatusMessage = Texts.Get(ModuleExplorerStringId.NameHasNoResRefCharacters);
            return;
        }

        if (creation.Exists(type, resRef))
        {
            StatusMessage = Texts.Get(ModuleExplorerStringId.ResourceAlreadyExists, resRef);
            return;
        }

        var options = await creation.ChooseOptionsAsync(type).ConfigureAwait(true);
        if (options == null)
            return;

        // Rechecked after the prompts: the builder was looking at a dialog while a pack could have started
        // behind it, and the write below is what a pack must not race.
        if (!CanCreateSelectedType)
        {
            StatusMessage = Texts.Get(ModuleExplorerStringId.CreateUnavailableDuringOperation);
            return;
        }

        var result = creation.Create(type, resRef, name.Trim(), options);
        if (!result.Succeeded)
        {
            StatusMessage = Texts.Get(ModuleExplorerStringId.CreateFailed, resRef, result.Problem);
            return;
        }

        var filed = FileNewResource(resRef, targetFolder);

        Log(Texts.Get(ModuleExplorerStringId.CreatedLog, section.SingularLabel.ToLowerInvariant(), resRef));
        creation.Created(type, resRef);
        Refresh();

        // Said plainly rather than left to be discovered: source the build compiles does nothing in game
        // until it has been compiled.
        if (filed)
        {
            StatusMessage = section.IsCompilable
                ? Texts.Get(ModuleExplorerStringId.CreatedNeedsCompile, resRef)
                : Texts.Get(ModuleExplorerStringId.Created, resRef);
        }

        if (CanOpenSelectedType)
            _host.Editors?.Open(type, resRef);
    }

    private void OpenCreationForm(IModuleExplorerCreation creation, ModuleResourceType type, CategoryFolder? targetFolder)
    {
        ActiveCreationForm = creation.OpenForm(
            type,
            new ModuleExplorerFormCallbacks(
                resRef =>
                {
                    ActiveCreationForm = null;
                    FileNewResource(resRef, targetFolder);
                    creation.Created(type, resRef);
                    Refresh();
                    _host.Editors?.Open(type, resRef);
                },
                () => ActiveCreationForm = null,
                // The form writes its files and then edits the module. A pack that starts between those
                // writes captures one without the other, so the form asks again at the moment it commits.
                () => !IsDeletingResource && _host.WriteGate?.IsLocked != true));
    }

    /// <summary>
    /// Files a freshly created resource into the folder captured when creation started, reporting a sidecar
    /// failure rather than letting the "Created ..." message overwrite it.
    /// </summary>
    /// <returns>
    /// False when the resource was created but could not be filed; the sidecar failure then stays on
    /// screen. True when there was nothing to file, or filing worked.
    /// </returns>
    private bool FileNewResource(string resRef, CategoryFolder? folder)
    {
        if (folder == null)
            return true;

        folder.AddMember(resRef);
        if (SaveCategories())
            return true;

        // The store restored the persisted catalog, so the file exists but is in Unsorted. Said
        // explicitly: silently filing it somewhere else is how a builder loses track of it.
        StatusMessage = Texts.Get(ModuleExplorerStringId.CreatedUnfiled, resRef, folder.Name, StatusMessage);
        return false;
    }

    // ----- folders -----

    [RelayCommand]
    private async Task NewFolderAsync()
    {
        var section = _host.Categories.Section(SelectedType);
        if (section == null || _host.Prompts is not { } prompts)
            return;

        var parent = SelectedRow?.Folder;
        var name = await prompts.PromptForTextAsync(
            parent == null
                ? Texts.Get(ModuleExplorerStringId.NewFolderHeadline)
                : Texts.Get(ModuleExplorerStringId.NewChildFolderHeadline, parent.Name),
            Texts.Get(ModuleExplorerStringId.FolderNameMessage),
            string.Empty,
            Texts.Get(ModuleExplorerStringId.CreateConfirm)).ConfigureAwait(true);

        if (string.IsNullOrWhiteSpace(name))
            return;

        // Checked rather than sanitized: the builder typed this and is still here to retype it. Asked
        // before the sibling check, so a name holding a separator is reported as that rather than as a
        // clash with whatever the split happened to land on.
        if (CategoryFolder.NameProblem(name) is { } problem)
        {
            StatusMessage = problem;
            return;
        }

        var trimmed = name.Trim();
        var nameAvailable = parent == null
            ? section.IsNameAvailable(trimmed)
            : parent.IsNameAvailable(trimmed);
        if (!nameAvailable)
        {
            StatusMessage = Texts.Get(ModuleExplorerStringId.FolderNameTaken, trimmed);
            return;
        }

        if (parent == null)
            section.AddFolder(trimmed);
        else
            parent.AddChild(trimmed);

        SaveCategories();
        Refresh();
    }

    [RelayCommand]
    private async Task RenameFolderAsync()
    {
        if (SelectedRow?.Folder is not { } folder || _host.Prompts is not { } prompts)
            return;

        var name = await prompts.PromptForTextAsync(
            Texts.Get(ModuleExplorerStringId.RenameFolderHeadline, folder.Name),
            Texts.Get(ModuleExplorerStringId.FolderNameMessage),
            folder.Name,
            Texts.Get(ModuleExplorerStringId.RenameConfirm)).ConfigureAwait(true);
        if (string.IsNullOrWhiteSpace(name) || name.Trim() == folder.Name)
            return;

        if (CategoryFolder.NameProblem(name) is { } problem)
        {
            StatusMessage = problem;
            return;
        }

        var section = _host.Categories.Section(SelectedType);
        var trimmed = name.Trim();
        if (section == null || !section.TryRenameFolder(folder, trimmed))
        {
            StatusMessage = Texts.Get(ModuleExplorerStringId.FolderNameTaken, trimmed);
            return;
        }

        SaveCategories();
        _history.Clear();
        Refresh();

        // Refresh rebuilds every node, so the pre-rebuild selection is orphaned. Rename mutates the folder
        // in place, so the same reference finds its rebuilt row.
        SelectedRow = Rows.FirstOrDefault(row => ReferenceEquals(row.Folder, folder));
    }

    /// <summary>
    /// Deletes a folder. Its contents are not: the members go back to Unsorted, because the sidecar only
    /// records an arrangement and deleting an arrangement must never delete a resource.
    /// </summary>
    [RelayCommand]
    private async Task DeleteFolderAsync()
    {
        var section = _host.Categories.Section(SelectedType);
        if (section == null || SelectedRow?.Folder is not { } folder || _host.Prompts is not { } prompts)
            return;

        // Sub-folders are named separately, because an empty branch of folders has no members at all:
        // without saying so, deleting it looks like a no-op right up until the arrangement is gone.
        var count = folder.MembersIncludingDescendants.Count();
        var subFolders = folder.Children.Count;
        if (count > 0 || subFolders > 0)
        {
            var confirmed = await prompts.ConfirmDestructiveAsync(
                Texts.Get(ModuleExplorerStringId.DeleteFolderHeadline, folder.Name),
                DeleteFolderDetail(count, subFolders),
                Texts.Get(ModuleExplorerStringId.DeleteFolderConfirm)).ConfigureAwait(true);

            if (!confirmed)
                return;
        }

        section.RemoveFolder(folder);
        SaveCategories();
        _history.Clear();
        SelectedRow = null;
        Refresh();
    }

    private string DeleteFolderDetail(int members, int subFolders)
    {
        var parts = new List<string>();
        if (subFolders > 0)
            parts.Add(Texts.Get(ModuleExplorerStringId.DeleteFolderSubfolders, subFolders));
        if (members > 0)
            parts.Add(Texts.Get(ModuleExplorerStringId.DeleteFolderMembers, members));

        return Texts.Get(
            ModuleExplorerStringId.DeleteFolderMessage,
            string.Join(Texts.Get(ModuleExplorerStringId.ListSeparator), parts));
    }

    private void PublishMoveTargets(CategorySection? section)
    {
        MoveTargets.Clear();
        if (section != null)
        {
            foreach (var folder in section.AllFolders())
                MoveTargets.Add(new ExplorerMoveTarget(folder, FolderPath(section, folder), MoveSelectedInto));
        }

        OnPropertyChanged(nameof(HasMoveTargets));
    }

    private string FolderPath(CategorySection section, CategoryFolder folder) =>
        string.Join(Texts.Get(ModuleExplorerStringId.FolderPathSeparator), section.PathTo(folder));

    /// <summary>Files the selected resource into a folder, which is how a builder organizes by hand.</summary>
    private void MoveSelectedInto(CategoryFolder target)
    {
        if (SelectedRow is not { } source)
            return;

        MoveResource(source, target);
    }

    /// <summary>Takes the selected resource out of every folder, back to Unsorted.</summary>
    [RelayCommand]
    private void RemoveFromFolder()
    {
        if (SelectedRow is not { } source)
            return;

        MoveResource(source, target: null);
    }

    /// <summary>
    /// Whether a resource row can be dropped on a folder row. The synthetic Unsorted row is a valid
    /// destination even though it has no folder behind it.
    /// </summary>
    public bool CanDropResource(ExplorerNodeViewModel? source, ExplorerNodeViewModel? target)
    {
        if (source?.Item == null || target?.IsBranch != true ||
            source.Type != SelectedType || target.Type != SelectedType)
        {
            return false;
        }

        var section = _host.Categories.Section(SelectedType);
        if (section == null || (target.Folder == null && !target.IsUnsorted))
            return false;

        var current = section.FoldersContaining(source.Item.ResRef).ToList();
        return target.Folder == null
            ? current.Count > 0
            : current.Count != 1 || !ReferenceEquals(current[0], target.Folder);
    }

    /// <summary>
    /// Commits a drag from a resource row to a real folder or to Unsorted. Source and destination are
    /// passed explicitly rather than read from selection: pointer movement and auto-scroll may change
    /// selection during a drag, and the item under the pointer must not become the item moved.
    /// </summary>
    public bool DropResource(ExplorerNodeViewModel? source, ExplorerNodeViewModel? target)
    {
        if (!CanDropResource(source, target))
            return false;

        return MoveResource(source!, target!.Folder);
    }

    private bool MoveResource(ExplorerNodeViewModel source, CategoryFolder? target)
    {
        var section = _host.Categories.Section(SelectedType);
        if (section == null || source.Item is not { } item || source.Type != SelectedType)
            return false;

        var before = CategoryMembershipMoves.FolderPathsContaining(section, item.ResRef);
        var after = target == null ? Array.Empty<string>() : new[] { section.PathKey(target) };
        var targetPath = target == null ? null : FolderPath(section, target);

        if (!CategoryMembershipMoves.MoveTo(section, item.ResRef, target))
            return false;

        var saved = SaveCategories();
        Refresh();
        if (!saved)
            return false;

        _history.Record(new CategoryMembershipEdit(SelectedType, item.ResRef, item.PrimaryText, before, after));
        StatusMessage = targetPath == null
            ? Texts.Get(ModuleExplorerStringId.MovedToUnsorted, item.PrimaryText)
            : Texts.Get(ModuleExplorerStringId.MovedToFolder, item.PrimaryText, targetPath);
        return true;
    }

    private bool CanUndoResourceMove() => _history.CanUndo;

    [RelayCommand(CanExecute = nameof(CanUndoResourceMove))]
    private void UndoResourceMove()
    {
        if (!_history.TryPeekUndo(out var edit) ||
            !ApplyResourceMove(edit, edit.BeforeFolderPaths, isUndo: true))
        {
            return;
        }

        _history.CompleteUndo();
        StatusMessage = Texts.Get(ModuleExplorerStringId.UndidMove, edit.DisplayName);
    }

    private bool CanRedoResourceMove() => _history.CanRedo;

    [RelayCommand(CanExecute = nameof(CanRedoResourceMove))]
    private void RedoResourceMove()
    {
        if (!_history.TryPeekRedo(out var edit) ||
            !ApplyResourceMove(edit, edit.AfterFolderPaths, isUndo: false))
        {
            return;
        }

        _history.CompleteRedo();
        StatusMessage = Texts.Get(ModuleExplorerStringId.RedidMove, edit.DisplayName);
    }

    private bool ApplyResourceMove(CategoryMembershipEdit edit, IReadOnlyList<string> destinationFolderPaths, bool isUndo)
    {
        var section = _host.Categories.Section(edit.Type);
        if (section == null)
        {
            _history.Clear();
            StatusMessage = Texts.Get(isUndo
                ? ModuleExplorerStringId.UndoSectionMissing
                : ModuleExplorerStringId.RedoSectionMissing);
            return false;
        }

        if (!CategoryMembershipMoves.TryResolve(section, destinationFolderPaths, out var destinations, out var missing))
        {
            _history.Clear();
            StatusMessage = Texts.Get(
                isUndo ? ModuleExplorerStringId.UndoFolderMissing : ModuleExplorerStringId.RedoFolderMissing,
                missing);
            Refresh();
            return false;
        }

        CategoryMembershipMoves.Apply(section, edit.ResRef, destinations);

        // Persisted even when the in-memory tree already matches the requested destination. This can be a
        // retry after a refused write: the history entry is intentionally retained, and success must mean
        // the sidecar on disk now matches the undo/redo state too.
        var saved = SaveCategories();
        Refresh();
        return saved;
    }

    private void NotifyMoveHistoryChanged()
    {
        UndoResourceMoveCommand.NotifyCanExecuteChanged();
        RedoResourceMoveCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Rename and delete notifications name a resource that may just have disappeared. A move edit for that
    /// identity can no longer be replayed safely: adding its stale resref back to the sidecar would corrupt
    /// membership while leaving the renamed file where it is.
    /// </summary>
    private void OnResourceChanged(ModuleResourceType type, string resRef)
    {
        if (_history.Involves(type, resRef) && _host.Content.IsModuleOpen && !_host.Content.Exists(type, resRef))
            _history.Clear();

        if (_search.Supports(type))
        {
            _search.Invalidate(type);
            _search.Queue(SelectedType, Filter);
        }
    }

    /// <summary>
    /// Writes the category sidecar and reports a refusal in the status line. The sidecar can legitimately
    /// decline - it is read-only when a newer toolset wrote it, and it will not clobber an external edit -
    /// and every command here has already told the builder what it did, so a silent refusal would mislead.
    /// </summary>
    private bool SaveCategories()
    {
        var result = _host.Categories.SaveChanges();
        if (!result.Saved)
            StatusMessage = result.Problem;

        return result.Saved;
    }

    // ----- deleting -----

    /// <summary>
    /// Deletes the logical resource the selected row stands for - every file the host says makes it up.
    /// The builder confirms first, the files are rechecked against what was confirmed, and the commit runs
    /// off the UI thread under the host's reservation.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanDeleteSelectedResource))]
    private async Task DeleteSelectedResourceAsync()
    {
        var row = SelectedRow;
        var item = row?.Item;
        if (row == null || item == null || !_host.Content.IsModuleOpen ||
            _host.Prompts is not { } prompts || _host.Deletion is not { } deletion)
        {
            return;
        }

        var type = row.Type;
        var resRef = item.ResRef;
        var displayName = string.IsNullOrWhiteSpace(item.Name) ? row.Name : item.Name;
        var kind = SectionFor(type).SingularLabel.ToLowerInvariant();
        var editors = _host.Editors;

        if (type == ModuleResourceType.Area && editors?.IsModulePropertiesOpen == true)
        {
            StatusMessage = Texts.Get(ModuleExplorerStringId.DeleteAreaModulePropertiesOpen);
            return;
        }

        var section = _host.Categories.Section(type);
        if (!CanUpdateMembership(section, resRef, displayName, kind))
            return;

        IModuleExplorerPreparedDeletion plan;
        try
        {
            plan = deletion.Prepare(type, resRef);
        }
        catch (Exception ex)
        {
            RefuseDelete(displayName, kind, resRef, ex.Message);
            return;
        }

        var closesOpenEditor = editors?.IsOpen(type, resRef) == true;
        var confirmed = await prompts.ConfirmDestructiveAsync(
            Texts.Get(ModuleExplorerStringId.DeleteHeadline, displayName),
            Texts.Get(closesOpenEditor
                ? ModuleExplorerStringId.DeleteMessageClosesEditor
                : ModuleExplorerStringId.DeleteMessage),
            Texts.Get(ModuleExplorerStringId.DeleteConfirm)).ConfigureAwait(true);
        if (!confirmed)
            return;

        // The warning is scoped to the editor state the builder confirmed. An editor that opened while the
        // dialog was displayed was not covered, so it is left intact and a second Delete must say so.
        if (!closesOpenEditor && editors?.IsOpen(type, resRef) == true)
        {
            StatusMessage = Texts.Get(ModuleExplorerStringId.DeleteOpenedDuringConfirmation, displayName);
            return;
        }

        // Reserved before rechecking editor ownership: the reservation blocks every editor-opening route
        // until the deletion and its catalog cleanup finish, closing the race between these checks and the
        // filesystem transaction.
        using var reservation = deletion.TryReserve();
        if (reservation == null)
        {
            StatusMessage = Texts.Get(ModuleExplorerStringId.DeleteWhileLocked, displayName);
            Log(Texts.Get(ModuleExplorerStringId.DeleteWhileLockedLog, kind, resRef));
            return;
        }

        if (!closesOpenEditor && editors?.IsOpen(type, resRef) == true)
        {
            StatusMessage = Texts.Get(ModuleExplorerStringId.DeleteOpenedDuringConfirmation, displayName);
            return;
        }

        if (type == ModuleResourceType.Area && editors?.IsModulePropertiesOpen == true)
        {
            StatusMessage = Texts.Get(ModuleExplorerStringId.DeleteAreaModulePropertiesNowOpen);
            return;
        }

        if (!CanUpdateMembership(section, resRef, displayName, kind))
            return;

        ModuleExplorerDeletionResult result;
        IsDeletingResource = true;
        StatusMessage = Texts.Get(ModuleExplorerStringId.Deleting, kind, displayName);
        try
        {
            result = await Task.Run(plan.Commit).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            StatusMessage = Texts.Get(ModuleExplorerStringId.DeleteRefused, displayName, ex.Message);
            Log(Texts.Get(ModuleExplorerStringId.DeleteFailedLog, kind, resRef, ex.Message));
            return;
        }
        finally
        {
            IsDeletingResource = false;
        }

        // The transaction committed, so the confirmed unsaved buffer can now be discarded. Keeping the
        // editor alive until this point means every refusal or failed commit leaves in-memory work intact.
        if (editors?.IsOpen(type, resRef) == true && !editors.TryCloseForDeletion(type, resRef))
            Log(Texts.Get(ModuleExplorerStringId.DeleteEditorNotClosedLog, kind, resRef));

        deletion.Deleted(type, resRef);

        var unfiled = true;
        if (section != null)
        {
            var folders = section.FoldersContaining(resRef).ToList();
            foreach (var folder in folders)
                folder.RemoveMember(resRef);

            if (folders.Count > 0)
                unfiled = SaveCategories();
        }

        SelectedRow = null;
        _history.Clear();
        Refresh();

        var cleanup = result.CleanupWarnings.Count == 0
            ? string.Empty
            : Texts.Get(
                ModuleExplorerStringId.DeleteCleanupWarning,
                string.Join(Texts.Get(ModuleExplorerStringId.CleanupSeparator), result.CleanupWarnings));
        StatusMessage = unfiled
            ? cleanup.TrimStart()
            : Texts.Get(ModuleExplorerStringId.DeletedStillFiled, kind, displayName, StatusMessage, cleanup);

        Log(Texts.Get(
            ModuleExplorerStringId.DeletedLog,
            kind,
            resRef,
            string.Join(Texts.Get(ModuleExplorerStringId.ListSeparator), result.DeletedPaths),
            cleanup));
    }

    /// <summary>
    /// A filed resource's delete also rewrites the sidecar. When that write would be refused, the delete is
    /// refused up front rather than leaving a folder listing a resource that is gone.
    /// </summary>
    private bool CanUpdateMembership(CategorySection? section, string resRef, string displayName, string kind)
    {
        if (section?.FoldersContaining(resRef).Any() != true)
            return true;

        var check = _host.Categories.CanSaveChanges();
        if (check.Saved)
            return true;

        RefuseDelete(displayName, kind, resRef, check.Problem);
        return false;
    }

    private void RefuseDelete(string displayName, string kind, string resRef, string? problem)
    {
        StatusMessage = Texts.Get(ModuleExplorerStringId.DeleteRefused, displayName, problem);
        Log(Texts.Get(ModuleExplorerStringId.DeleteRefusedLog, kind, resRef, problem));
    }

    // ----- browsing -----

    [RelayCommand(CanExecute = nameof(CanCompileSelectedType))]
    private async Task CompileSelected()
    {
        if (SelectedRow?.Item is not { } item || !SelectedSection.IsCompilable)
            return;

        if (!CanCompileSelectedType)
        {
            StatusMessage = Texts.Get(ModuleExplorerStringId.CompileUnavailable);
            return;
        }

        if (_host.Editors is not { } editors)
            return;

        StatusMessage = Texts.Get(ModuleExplorerStringId.Compiling, item.ResRef);
        await editors.CompileAsync(SelectedType, item.ResRef).ConfigureAwait(true);
        StatusMessage = null;
    }

    /// <summary>The context menu's Open, which is the double-click by another route.</summary>
    [RelayCommand(CanExecute = nameof(CanOpenSelectedType))]
    private void OpenSelected() => OpenSelectedItem();

    /// <summary>Double-click: open a resource, or expand a folder.</summary>
    public void OpenSelectedItem()
    {
        if (SelectedRow is not { } row)
            return;

        if (row.IsBranch)
        {
            Toggle(row);
            return;
        }

        if (!CanOpenSelectedType)
        {
            StatusMessage = IsDeletingResource
                ? Texts.Get(ModuleExplorerStringId.OpenUnavailableWhileDeleting)
                : Texts.Get(ModuleExplorerStringId.CannotOpenType, SelectedSection.Label);
            return;
        }

        _host.Editors?.Open(row.Type, row.ResRef);
    }

    [RelayCommand]
    private void Toggle(ExplorerNodeViewModel? row)
    {
        if (row is not { IsBranch: true })
            return;

        row.IsExpanded = !row.IsExpanded;
        PublishVisibleRows();
    }

    partial void OnFilterChanged(string value)
    {
        _search.Queue(SelectedType, Filter);
        Refresh();
    }

    partial void OnSelectedRowChanged(ExplorerNodeViewModel? value)
    {
        OnPropertyChanged(nameof(HasFolderSelected));
        OnPropertyChanged(nameof(CanDeleteSelectedResource));
        DeleteSelectedResourceCommand.NotifyCanExecuteChanged();

        if (value?.Item is { } item)
            _host.Selection?.Selected(value.Type, item);
    }

    partial void OnIsDeletingResourceChanged(bool value)
    {
        OnPropertyChanged(nameof(CanCreateSelectedType));
        OnPropertyChanged(nameof(CanOpenSelectedType));
        OnPropertyChanged(nameof(CanCompileSelectedType));
        OnPropertyChanged(nameof(CanDeleteSelectedResource));
        NewItemCommand.NotifyCanExecuteChanged();
        OpenSelectedCommand.NotifyCanExecuteChanged();
        CompileSelectedCommand.NotifyCanExecuteChanged();
        DeleteSelectedResourceCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Re-reads every write-dependent capability after the module-wide lock flips.</summary>
    private void OnWriteGateChanged()
    {
        OnPropertyChanged(nameof(CanCreateSelectedType));
        OnPropertyChanged(nameof(CanCompileSelectedType));
        OnPropertyChanged(nameof(CanDeleteSelectedResource));
        NewItemCommand.NotifyCanExecuteChanged();
        CompileSelectedCommand.NotifyCanExecuteChanged();
        DeleteSelectedResourceCommand.NotifyCanExecuteChanged();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _search.Dispose();
        _history.Changed -= NotifyMoveHistoryChanged;
        if (_host.WriteGate is { } writeGate)
            writeGate.Changed -= OnWriteGateChanged;
        _host.Content.ResourceChanged -= OnResourceChanged;
        _host.Content.ContentChanged -= Refresh;
    }

    // ----- tree assembly -----

    /// <summary>
    /// Seeds a never-organized section with the host's starting folders, once. Written into the sidecar
    /// rather than recomputed, which is what makes it editable, and seeded from the unfiltered list so what
    /// a search happens to be showing cannot decide the shape.
    /// </summary>
    private void SeedIfNeeded(CategorySection section, IReadOnlyList<ExplorerItem> items)
    {
        // IsSeeded, not just "has folders": a builder who deliberately empties a section keeps that flag
        // so the empty arrangement survives a restart instead of being re-seeded.
        if (_host.Organization is not { } organization ||
            !_seeded.Add(SelectedType) ||
            section.IsSeeded ||
            section.Folders.Count > 0 ||
            items.Count == 0)
        {
            return;
        }

        // The names seeding files by may still be loading; seeding off bare resrefs would put everything
        // in Unsorted and then never try again.
        if (!organization.IsReadyToSeed(SelectedType))
        {
            _seeded.Remove(SelectedType);
            return;
        }

        var seeded = organization.Seed(section, SelectedType, items);
        if (seeded == 0)
            return;

        section.IsSeeded = true;
        SaveCategories();
        Log(Texts.Get(ModuleExplorerStringId.OrganisedLog, SelectedSection.Label.ToLowerInvariant(), seeded));
    }

    private IReadOnlyList<ExplorerItem> Filtered(IReadOnlyList<ExplorerItem> items)
    {
        if (string.IsNullOrWhiteSpace(Filter))
            return items;

        return _search.Filter(SelectedType, items, Filter.Trim());
    }

    private ExplorerSection SectionFor(ModuleResourceType type) =>
        Sections.FirstOrDefault(section => section.Type == type)
        ?? new ExplorerSection(type, type.ToString(), type.ToString());

    private void Log(string message) => _host.Log?.Write(message);

    private static IEnumerable<ExplorerNodeViewModel> Flatten(ExplorerNodeViewModel node) =>
        new[] { node }.Concat(node.Children.SelectMany(Flatten));

    private void PublishVisibleRows()
    {
        Rows.Clear();
        foreach (var root in _roots)
            Publish(root);
    }

    private void Publish(ExplorerNodeViewModel node)
    {
        // Filtered folder counts already include every matching descendant, so a zero means nothing
        // beneath can lead to a result. The full tree stays in _roots so clearing the search restores its
        // expansion, but dead-end folders (including an empty Unsorted) are not published while searching.
        if (!string.IsNullOrWhiteSpace(Filter) && node.IsBranch && node.Count == 0)
            return;

        Rows.Add(node);
        if (!node.IsExpanded)
            return;

        foreach (var child in node.Children)
            Publish(child);
    }
}
