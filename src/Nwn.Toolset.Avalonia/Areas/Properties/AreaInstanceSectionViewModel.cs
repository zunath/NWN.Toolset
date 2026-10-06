using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nwn.Authoring.Areas.Placement;
using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Editing;
using Nwn.Authoring.Resources;
using Nwn.Preview.Areas;
using Nwn.Preview.Areas.Clipboard;
using Nwn.Toolset.Avalonia.Doors;
using Nwn.Toolset.Avalonia.Sounds;
using Nwn.Toolset.Avalonia.Triggers;
using Nwn.Toolset.Avalonia.Variables;
using Nwn.Toolset.Avalonia.Waypoints;

namespace Nwn.Toolset.Avalonia.Areas.Properties;

/// <summary>
/// One expandable section of the area Properties page: the placed-instance list for a single
/// blueprint type (e.g. "Creatures"). Lists the placed instances in a grid, edits the selected
/// instance's tag/position/heading and its typed editor or local-variable table, and supports Add
/// (via a palette browser + InstanceFieldMap), Duplicate, and Delete - all through the host's
/// transaction on the area's GIT session, with the paired GIC comment kept index-aligned.
/// </summary>
public partial class AreaInstanceSectionViewModel : AreaInstanceDetailState, IDisposable
{
    private readonly AreaInstanceSectionHost _host;
    private readonly DocumentSession _gitSession;
    private readonly DocumentSession _gicSession;
    private readonly ModuleResourceType _blueprintType;
    private readonly string _listFieldName;
    private readonly Func<string, Action, bool> _runEdit;
    private readonly AreaPropertiesTexts _texts;

    public string Title { get; }

    /// <summary>The blueprint type this section's list holds (e.g. Utc for Creatures).</summary>
    public ModuleResourceType BlueprintType => _blueprintType;

    /// <summary>The GIT list field this section edits.</summary>
    public string ListFieldName => _listFieldName;

    public ObservableCollection<InstanceRow> Rows { get; } = new();

    [ObservableProperty]
    private InstanceRow? _selectedRow;

    [ObservableProperty]
    private bool _hasSelection;

    /// <summary>
    /// Whether this kind's placed-instance details are open on the area's Properties page.
    /// Kept on the view model so switching document tabs does not collapse the builder's work.
    /// </summary>
    [ObservableProperty]
    private bool _isExpanded;

    /// <summary>True for a door section whose host supplies a typed door editor.</summary>
    public bool UsesDoorEditor => _blueprintType == ModuleResourceType.Utd && _host.Editors != null;

    public override bool UsesGenericDetailEditor =>
        !UsesDoorEditor && !HasWaypointBehaviorEditor && !HasSoundBehaviorEditor && !HasTriggerBehaviorEditor;

    [ObservableProperty]
    private DoorBehaviorEditorViewModel? _doorEditor;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasWaypointBehaviorEditor))]
    [NotifyPropertyChangedFor(nameof(UsesGenericDetailEditor))]
    private WaypointBehaviorEditorViewModel? _waypointEditor;

    public bool HasWaypointBehaviorEditor => WaypointEditor != null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSoundBehaviorEditor))]
    [NotifyPropertyChangedFor(nameof(UsesGenericDetailEditor))]
    private SoundBehaviorEditorViewModel? _soundEditor;

    public bool HasSoundBehaviorEditor => SoundEditor != null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTriggerBehaviorEditor))]
    [NotifyPropertyChangedFor(nameof(UsesGenericDetailEditor))]
    private TriggerBehaviorEditorViewModel? _triggerEditor;

    public bool HasTriggerBehaviorEditor => TriggerEditor != null;

    [ObservableProperty]
    private VarTableSectionViewModel? _varTableSection;

    [ObservableProperty]
    private PaletteBrowserViewModel? _activePaletteBrowser;

    public string AddLabel => _texts.Get(AreaPropertiesStringId.AddInstance);
    public string DuplicateLabel => _texts.Get(AreaPropertiesStringId.DuplicateInstance);
    public string DeleteLabel => _texts.Get(AreaPropertiesStringId.DeleteInstance);
    public string LocalVariablesHeader => _texts.Get(AreaPropertiesStringId.LocalVariablesHeader);

    /// <summary>The grid's column captions: tag, template, X, Y and Z.</summary>
    public IReadOnlyList<string> ColumnHeaders { get; }

    public AreaInstanceSectionViewModel(
        string title,
        string listFieldName,
        ModuleResourceType blueprintType,
        AreaInstanceSectionHost host)
        : base(
            blueprintType,
            (host ?? throw new ArgumentNullException(nameof(host))).RunEdit,
            DetailLabels(host.Texts),
            new AreaInstanceDetailEditDescriptions(
                host.Texts.Get(AreaPropertiesStringId.EditTag, title),
                host.Texts.Get(AreaPropertiesStringId.EditMove, title),
                host.Texts.Get(AreaPropertiesStringId.EditRotate, title),
                host.Texts.Get(AreaPropertiesStringId.EditResize, title)))
    {
        Title = title;
        _host = host;
        _texts = host.Texts;
        _listFieldName = listFieldName;
        _blueprintType = blueprintType;
        _gitSession = host.Instances;
        _gicSession = host.Comments;
        _runEdit = host.RunEdit;
        ColumnHeaders =
        [
            _texts.Get(AreaPropertiesStringId.ColumnTag),
            _texts.Get(AreaPropertiesStringId.ColumnTemplate),
            _texts.Get(AreaPropertiesStringId.ColumnX),
            _texts.Get(AreaPropertiesStringId.ColumnY),
            _texts.Get(AreaPropertiesStringId.ColumnZ),
        ];
        EditApplied += OnInstanceDetailEditApplied;

        RefreshFromDocument();
    }

    /// <summary>
    /// Raised once at the end of every <see cref="RefreshFromDocument"/>.
    /// </summary>
    /// <remarks>
    /// One signal for "this list changed", whatever moved it - an add, a delete, an undo, a redo,
    /// or a reload after an external edit. An Area Contents panel rebuilds its tree from this
    /// rather than watching <see cref="Rows"/>, which reports a clear plus one add per row and so
    /// would rebuild once per row for one refresh of a busy area.
    /// </remarks>
    public event Action? RowsRefreshed;

    /// <summary>Refreshes a selected typed instance editor after its palette changes.</summary>
    public void RefreshPaletteChoices()
    {
        DoorEditor?.RefreshPaletteChoices();
        WaypointEditor?.RefreshPaletteChoices();
        SoundEditor?.RefreshPaletteChoices();
        TriggerEditor?.RefreshPaletteChoices();
    }

    /// <summary>Refreshes TLK-backed choices in the selected typed placement editor.</summary>
    public void RefreshTlkLabels()
    {
        DoorEditor?.RefreshTlkLabels();
        WaypointEditor?.RefreshTlkLabels();
        SoundEditor?.RefreshTlkLabels();
        TriggerEditor?.RefreshTlkLabels();
    }

    /// <summary>Applies save-time normalization required by the selected typed editor.</summary>
    public bool PrepareForSave()
    {
        if (WaypointEditor?.PrepareForSave() == false)
            return false;

        return !HasSingletonWaypointTagConflicts();
    }

    private bool HasSingletonWaypointTagConflicts()
    {
        if (_blueprintType != ModuleResourceType.Utw || _host.WaypointTags is not { } policy)
            return false;

        var list = _gitSession.Document.Root.GetOrNull(_listFieldName)?.Elements;
        if (list == null)
            return false;

        var singletonTags = list
            .Select(policy.ResolveTag)
            .OfType<string>()
            .Where(policy.IsSingletonTag)
            .ToList();
        if (singletonTags
            .GroupBy(tag => tag, StringComparer.OrdinalIgnoreCase)
            .Any(group => group.Count() > 1))
        {
            return true;
        }

        return singletonTags.Any(tag => policy.CountPlacementsOutsideArea(tag) > 0);
    }

    /// <summary>Rebuilds the grid rows from the current document state for initial load,
    /// structural edits, and undo/redo. Detail-field edits update the selected row in place
    /// so typing does not recreate every row in a large area.</summary>
    public void RefreshFromDocument()
    {
        var selectedIndex = SelectedRow?.Index;
        Rows.Clear();

        var listField = _gitSession.Document.Root.GetOrNull(_listFieldName);
        if (listField?.Elements != null)
        {
            for (var i = 0; i < listField.Elements.Count; i++)
            {
                var element = listField.Elements[i];
                var (x, y, z) = InstanceFieldMap.GetPosition(_blueprintType, element);
                Rows.Add(new InstanceRow(
                    i,
                    InstanceFieldMap.GetTag(element) ?? string.Empty,
                    InstanceFieldMap.GetTemplateResRef(_blueprintType, element) ?? string.Empty,
                    x, y, z,
                    InstanceFieldMap.GetDisplayName(_blueprintType, element) ?? string.Empty));
            }
        }

        SelectedRow = selectedIndex.HasValue && selectedIndex.Value < Rows.Count
            ? Rows[selectedIndex.Value]
            : null;

        RowsRefreshed?.Invoke();
    }

    partial void OnSelectedRowChanged(InstanceRow? value)
    {
        HasSelection = value != null;
        var element = value != null ? GetElement(value.Index) : null;
        if (element == null)
        {
            SetInstance(null);
            DoorEditor?.Dispose();
            DoorEditor = null;
            WaypointEditor?.Dispose();
            WaypointEditor = null;
            VarTableSection = null;
            SoundEditor?.Dispose();
            SoundEditor = null;
            TriggerEditor?.Dispose();
            TriggerEditor = null;
            return;
        }

        SetInstance(element);

        DoorEditor?.Dispose();
        DoorEditor = null;
        WaypointEditor?.Dispose();
        WaypointEditor = null;
        SoundEditor?.Dispose();
        SoundEditor = null;
        TriggerEditor?.Dispose();
        TriggerEditor = null;

        var editors = _host.Editors;
        if (UsesDoorEditor && editors!.CreateDoor(element, RunSpecializedEdit, _gitSession.UndoStack.IsDirty) is { } door)
        {
            DoorEditor = door;
            VarTableSection = null;
        }
        else if (_blueprintType == ModuleResourceType.Utw &&
                 editors?.CreateWaypoint(element, RunSpecializedEdit,
                     tag => IsSingletonWaypointTagInUse(value!.Index, tag)) is { } waypoint)
        {
            VarTableSection = null;
            WaypointEditor = waypoint;
        }
        else if (_blueprintType == ModuleResourceType.Uts &&
                 editors?.CreateSound(element, _runEdit) is { } sound)
        {
            VarTableSection = null;
            SoundEditor = sound;
            SoundEditor.ValueChanged += () =>
            {
                if (SelectedRow is { } row)
                    row.Tag = InstanceFieldMap.GetTag(element) ?? string.Empty;
            };
        }
        else if (_blueprintType == ModuleResourceType.Utt &&
                 editors?.CreateTrigger(element, RunSpecializedEdit) is { } trigger)
        {
            VarTableSection = null;
            TriggerEditor = trigger;
        }
        else
        {
            Func<string, Action, bool> runEdit = (description, mutation) => _runEdit(description, mutation);
            VarTableSection = editors?.CreateVariables(runEdit, new VarTable(element))
                ?? new VarTableSectionViewModel(runEdit, new VarTable(element));
        }

        OnPropertyChanged(nameof(UsesGenericDetailEditor));
    }

    private bool IsSingletonWaypointTagInUse(int currentIndex, string tag)
    {
        if (_host.WaypointTags is not { } policy || !policy.IsSingletonTag(tag))
            return false;

        if (policy.CountPlacementsOutsideArea(tag) > 0)
            return true;

        var list = _gitSession.Document.Root.GetOrNull(_listFieldName)?.Elements;
        if (list == null)
            return false;

        for (var index = 0; index < list.Count; index++)
        {
            if (index == currentIndex)
                continue;

            var otherTag = policy.ResolveTag(list[index]);
            if (string.Equals(otherTag, tag, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private bool RunSpecializedEdit(string description, Action mutation)
    {
        if (!_runEdit(description, mutation))
            return false;

        if (SelectedRow is { } row && GetElement(row.Index) is { } element)
        {
            row.Tag = InstanceFieldMap.GetTag(element) ?? string.Empty;
            row.TemplateResRef =
                InstanceFieldMap.GetTemplateResRef(_blueprintType, element) ?? string.Empty;
            SetInstance(element);
        }

        return true;
    }

    private void OnInstanceDetailEditApplied()
    {
        if (SelectedRow is not { } row || CurrentInstance is not { } instance ||
            !ReferenceEquals(GetElement(row.Index), instance))
        {
            return;
        }

        row.Tag = InstanceFieldMap.GetTag(instance) ?? string.Empty;
        row.X = (float)DetailX;
        row.Y = (float)DetailY;
        row.Z = (float)DetailZ;
    }

    [RelayCommand]
    private void Add() => OpenPaletteBrowser(AddFromPalette, () => { });

    /// <summary>
    /// Opens this section's palette browser - the same flow this section's own "Add..." uses - and
    /// invokes <paramref name="onResRefChosen"/> once a blueprint is picked, or
    /// <paramref name="onCancelled"/> if the browser is dismissed instead. A scene "Place..." flow
    /// reuses this exact path rather than a parallel one, so both entry points browse identically.
    /// </summary>
    public void OpenPaletteBrowser(Action<string> onResRefChosen, Action onCancelled)
    {
        Action<string> complete = resRef =>
        {
            ActivePaletteBrowser = null;
            onResRefChosen(resRef);
        };
        Action cancel = () =>
        {
            ActivePaletteBrowser = null;
            onCancelled();
        };

        if (ActivePaletteBrowser is { } activeBrowser)
        {
            activeBrowser.RebindCompletionActions(complete, cancel);
            return;
        }

        if (_host.Palettes is not { } palettes)
            return;

        if (!palettes.TryLocate(_blueprintType, out var source))
        {
            _host.Log?.Write(_texts.Get(AreaPropertiesStringId.NoPaletteFile, Title, source));
            return;
        }

        ActivePaletteBrowser = new PaletteBrowserViewModel(
            Title,
            source,
            () => palettes.Read(source),
            complete,
            cancel,
            _host.Log,
            _host.ResolveStrRef,
            _texts);
    }

    /// <summary>Dismisses the palette picker, if one is open.</summary>
    public void ClosePalette() => ActivePaletteBrowser = null;

    public void Dispose()
    {
        SetInstance(null);
        EditApplied -= OnInstanceDetailEditApplied;
        DoorEditor?.Dispose();
        DoorEditor = null;
        WaypointEditor?.Dispose();
        WaypointEditor = null;
        SoundEditor?.Dispose();
        SoundEditor = null;
        TriggerEditor?.Dispose();
        TriggerEditor = null;
        ActivePaletteBrowser = null;
    }

    private void AddFromPalette(string resRef) => AddInstanceAt(resRef, 0f, 0f, 0f);

    /// <summary>
    /// Creates a new instance from <paramref name="resRef"/>'s blueprint at the given
    /// placement (via <see cref="InstanceFieldMap.CreateInstance"/>) and inserts it as one
    /// transaction - the exact path this section's own "Add..." (at the origin) and a scene
    /// "Place..." flow (at the clicked ground position) both use.
    /// </summary>
    public bool AddInstanceAt(
        string resRef,
        float x,
        float y,
        float z,
        float xOrientation = 1f,
        float yOrientation = 0f,
        bool useIndexedBlueprint = false)
    {
        try
        {
            var blueprint = _host.Blueprints.LoadBlueprint(_blueprintType, resRef, useIndexedBlueprint);
            var ok = _runEdit(_texts.Get(AreaPropertiesStringId.EditAdd, Title), () =>
            {
                var listField = _gitSession.Document.Root.GetOrNull(_listFieldName);
                if (listField == null)
                {
                    listField = JsonGffField.CreateList();
                    _gitSession.Document.Root.Add(_listFieldName, listField);
                }

                var instance = InstanceFieldMap.CreateInstance(
                    _blueprintType, blueprint, resRef,
                    x, y, z, xOrientation, yOrientation);
                var insertAt = listField.Elements!.Count;
                listField.InsertElement(insertAt, instance);
                new GicDocument(_gicSession.Document)
                    .InsertBlankComment(
                        _listFieldName,
                        _blueprintType,
                        insertAt,
                        listField.Elements.Count);
            });

            if (ok)
                RefreshFromDocument();

            return ok;
        }
        catch (Exception ex)
        {
            _host.Log?.Write(_texts.Get(AreaPropertiesStringId.AddFailed, Title, resRef, ex.Message));
            return false;
        }
    }

    /// <summary>
    /// Inserts a copied instance at a new map position while preserving every other authored GIT
    /// field and its paired GIC comment.
    /// </summary>
    public bool AddCopiedInstanceAt(
        AreaInstanceClipboardEntry copy,
        float x,
        float y,
        float z,
        float xOrientation,
        float yOrientation)
    {
        ArgumentNullException.ThrowIfNull(copy);
        if (copy.Type != _blueprintType ||
            !string.Equals(copy.ModuleRoot, _host.Blueprints.ModuleIdentity, StringComparison.OrdinalIgnoreCase))
            return false;

        try
        {
            var ok = _runEdit(_texts.Get(AreaPropertiesStringId.EditPaste, Title), () =>
            {
                var listField = _gitSession.Document.Root.GetOrNull(_listFieldName);
                if (listField == null)
                {
                    listField = JsonGffField.CreateList();
                    _gitSession.Document.Root.Add(_listFieldName, listField);
                }

                var instance = InstanceFieldMap.Duplicate(copy.Instance);
                InstanceFieldMap.SetPosition(_blueprintType, instance, x, y, z);
                InstanceFieldMap.SetOrientation(
                    _blueprintType, instance, xOrientation, yOrientation);

                var insertAt = listField.Elements!.Count;
                listField.InsertElement(insertAt, instance);
                new GicDocument(_gicSession.Document).InsertCopiedComment(
                    _listFieldName,
                    _blueprintType,
                    insertAt,
                    listField.Elements.Count,
                    copy.Comment);
            });

            if (ok)
                RefreshFromDocument();

            return ok;
        }
        catch (Exception ex)
        {
            _host.Log?.Write(_texts.Get(AreaPropertiesStringId.PasteFailed, Title, ex.Message));
            return false;
        }
    }

    /// <summary>
    /// Sets the position of the instance at <paramref name="index"/> through
    /// <see cref="InstanceFieldMap.SetPosition"/> - the exact setter the detail form's X/Y/Z
    /// editors use - as one transaction, so a scene drag produces the identical diff shape a
    /// detail-form edit would.
    /// </summary>
    public bool SetInstancePosition(int index, float x, float y, float z, string? description = null)
    {
        var element = GetElement(index);
        if (element == null)
            return false;

        var ok = _runEdit(description ?? _texts.Get(AreaPropertiesStringId.EditMove, Title),
            () => InstanceFieldMap.SetPosition(_blueprintType, element, x, y, z));

        if (ok)
            RefreshFromDocument();

        return ok;
    }

    /// <summary>Mirrors <see cref="SetInstancePosition"/> for heading, via <see cref="InstanceFieldMap.SetOrientation"/>.</summary>
    public bool SetInstanceOrientation(int index, float xOrientation, float yOrientation, string? description = null)
    {
        var element = GetElement(index);
        if (element == null)
            return false;

        var ok = _runEdit(description ?? _texts.Get(AreaPropertiesStringId.EditRotate, Title),
            () => InstanceFieldMap.SetOrientation(_blueprintType, element, xOrientation, yOrientation));

        if (ok)
            RefreshFromDocument();

        return ok;
    }

    /// <summary>
    /// Sets position and heading together as one document transaction. Door snapping uses this
    /// because the doorway position and orientation are one invariant: undoing only one of them
    /// leaves the door stranded sideways in its previous frame.
    /// </summary>
    public bool SetInstanceTransform(
        int index,
        float x,
        float y,
        float z,
        float xOrientation,
        float yOrientation,
        string? description = null)
    {
        var element = GetElement(index);
        if (element == null)
            return false;

        var ok = _runEdit(description ?? _texts.Get(AreaPropertiesStringId.EditMove, Title), () =>
        {
            InstanceFieldMap.SetPosition(_blueprintType, element, x, y, z);
            InstanceFieldMap.SetOrientation(_blueprintType, element, xOrientation, yOrientation);
        });

        if (ok)
            RefreshFromDocument();

        return ok;
    }

    [RelayCommand]
    private void Duplicate()
    {
        if (SelectedRow is not { } row)
            return;

        var element = GetElement(row.Index);
        if (element == null)
            return;

        _runEdit(_texts.Get(AreaPropertiesStringId.EditDuplicate, Title), () =>
        {
            var listField = _gitSession.Document.Root.Get(_listFieldName);
            var clone = InstanceFieldMap.Duplicate(element);
            listField.InsertElement(row.Index + 1, clone);
            new GicDocument(_gicSession.Document).DuplicateComment(
                _listFieldName,
                _blueprintType,
                row.Index,
                listField.Elements!.Count);
        });

        RefreshFromDocument();
    }

    [RelayCommand]
    private void Delete()
    {
        if (SelectedRow is { } row)
            DeleteInstances(new[] { row.Index });
    }

    /// <summary>
    /// Removes the placements at <paramref name="indices"/> as one transaction, so a whole group
    /// deleted from the Area Contents tree is one undo rather than one per object.
    /// </summary>
    /// <remarks>
    /// Removal runs highest index first. Ascending order is wrong and wrong quietly: every index
    /// after the first has shifted down by one, so the second removal takes its neighbour and the
    /// last one runs off the end of the list.
    /// </remarks>
    public bool DeleteInstances(IReadOnlyList<int> indices)
    {
        if (indices == null || indices.Count == 0)
            return false;

        var ordered = indices.Distinct().OrderByDescending(index => index).ToList();
        var description = ordered.Count == 1
            ? _texts.Get(AreaPropertiesStringId.EditDeleteOne, Title)
            : _texts.Get(AreaPropertiesStringId.EditDeleteMany, ordered.Count, Title);

        var ok = _runEdit(description, () =>
        {
            var listField = _gitSession.Document.Root.Get(_listFieldName);
            var comments = new GicDocument(_gicSession.Document);

            foreach (var index in ordered)
            {
                if (listField.Elements == null || index < 0 || index >= listField.Elements.Count)
                    continue;

                listField.RemoveElementAt(index);
                comments.RemoveComment(
                    _listFieldName, _blueprintType, index, listField.Elements.Count);
            }
        });

        if (!ok)
            return false;

        SelectedRow = null;
        RefreshFromDocument();
        return true;
    }

    private JsonGffStruct? GetElement(int index)
    {
        var listField = _gitSession.Document.Root.GetOrNull(_listFieldName);
        if (listField?.Elements == null || index < 0 || index >= listField.Elements.Count)
            return null;

        return listField.Elements[index];
    }

    /// <summary>
    /// The authored instance at a row index, for the host's single-marker scene update. The
    /// struct is the live document's; callers must not mutate it outside a transaction.
    /// </summary>
    public JsonGffStruct? GetInstanceForScene(int index) => GetElement(index);

    /// <summary>
    /// Takes an independent clipboard snapshot of one GIT instance and its aligned GIC comment.
    /// </summary>
    public AreaInstanceClipboardEntry? CopyInstanceForPlacement(
        int index,
        InstanceMarker preview)
    {
        var instance = GetElement(index);
        if (instance == null)
            return null;

        var comments = _gicSession.Document.Root.GetOrNull(_listFieldName)?.Elements;
        var comment = comments != null && index >= 0 && index < comments.Count
            ? InstanceFieldMap.Duplicate(comments[index])
            : null;

        return new AreaInstanceClipboardEntry(
            _host.Blueprints.ModuleIdentity,
            _blueprintType,
            InstanceFieldMap.Duplicate(instance),
            comment,
            preview);
    }

    private static AreaInstanceDetailLabels DetailLabels(AreaPropertiesTexts texts) => new(
        texts.Get(AreaPropertiesStringId.DetailTag),
        texts.Get(AreaPropertiesStringId.DetailX),
        texts.Get(AreaPropertiesStringId.DetailY),
        texts.Get(AreaPropertiesStringId.DetailZ),
        texts.Get(AreaPropertiesStringId.DetailFacingX),
        texts.Get(AreaPropertiesStringId.DetailFacingY),
        texts.Get(AreaPropertiesStringId.DetailWidth),
        texts.Get(AreaPropertiesStringId.DetailHeight));
}
