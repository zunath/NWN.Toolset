using System.Collections.ObjectModel;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nwn.Authoring.Behaviors;
using Nwn.Authoring.Doors;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Toolset.Avalonia.Appearances;
using Nwn.Toolset.Avalonia.Behaviors;
using Nwn.Toolset.Avalonia.Variables;

namespace Nwn.Toolset.Avalonia.Doors;

/// <summary>The behavior-shaped door editor shared by blueprints and placements.</summary>
/// <remarks>
/// The host supplies the behaviors, Basic rows, door conventions, choices, appearances and key items
/// through <see cref="DoorBehaviorEditorHost"/>; a host with a model preview derives from this class
/// and supplies <see cref="PreviewView"/>.
/// </remarks>
public partial class DoorBehaviorEditorViewModel : ObservableObject, IDisposable
{
    private readonly DoorBehaviorEditorHost _host;
    private readonly Func<string, Action, bool> _runEdit;
    private readonly IReadOnlyList<DoorAppearanceChoice> _appearances;
    private readonly bool _isInstance;
    private bool _disposed;

    public ObservableCollection<BehaviorListItemViewModel> BehaviorList { get; } = new();

    public ObservableCollection<DoorRowViewModel> BasicRows { get; } = new();

    public ObservableCollection<DoorRowViewModel> BehaviorRows { get; } = new();

    public AppearanceGalleryViewModel Appearance { get; }

    [ObservableProperty]
    private VarTableSectionViewModel? _variables;

    [ObservableProperty]
    private DoorBehavior _behavior;

    [ObservableProperty]
    private bool _isDirty;

    /// <summary>The editor's captions.</summary>
    public BehaviorEditorTexts Texts => _host.Texts;

    public string HeaderName => Behavior.DisplayName;

    public string HeaderKind => _host.Texts.Get(_isInstance
        ? BehaviorEditorStringId.KindInstance
        : BehaviorEditorStringId.KindBlueprint);

    public string HeaderOwner { get; private set; }

    public void SetHeaderOwner(string value)
    {
        HeaderOwner = value;
        OnPropertyChanged(nameof(HeaderOwner));
    }

    public bool ShowsVariablesTab => Behavior.AllowsVariables;

    public string? Incomplete { get; private set; }

    public bool IsIncomplete => Incomplete != null;

    public string AppearanceDescription => Appearance.CurrentDescription;

    public string DoorTag => Store.GetString(BehaviorFieldStorage.Field, "Tag");

    public string TemplateResRef => Store.GetString(BehaviorFieldStorage.Field, "TemplateResRef");

    /// <summary>The model preview drawn beside the tabs; nothing when the host has none.</summary>
    public virtual Control? PreviewView => null;

    /// <summary>The door this editor edits.</summary>
    public JsonGffStruct Door => Store.Door;

    /// <summary>True for a placement, false for a blueprint.</summary>
    public bool IsInstance => _isInstance;

    protected DoorBehaviorValueStore Store { get; }

    protected bool IsDisposed => _disposed;

    public DoorBehaviorEditorViewModel(
        JsonGffStruct door,
        string headerOwner,
        bool isInstance,
        Func<string, Action, bool> runEdit,
        DoorBehaviorEditorHost host,
        bool isDirty = false)
    {
        ArgumentNullException.ThrowIfNull(door);
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _runEdit = runEdit ?? throw new ArgumentNullException(nameof(runEdit));
        Store = new DoorBehaviorValueStore(door, host.Catalog.Conventions);
        _appearances = host.Appearances;
        _isInstance = isInstance;
        _behavior = host.Catalog.Custom;
        HeaderOwner = headerOwner;
        IsDirty = isDirty;

        // The same grid the creature editor picks its appearance from. Doors and creatures
        // want exactly the same thing - search a table, look at the pictures, click one.
        var entries = _appearances
            .Select(choice => new DoorAppearanceGalleryEntry(
                choice, new AppearanceGalleryOptionId(AppearanceKey(choice))))
            .ToList();
        var options = entries
            .Select(entry => new AppearanceGalleryOption(entry.OptionId, entry.Choice.Display, entry.Choice.Model))
            .ToList();
        var previews = host.AppearancePreviews?.Create(entries);
        Appearance = new AppearanceGalleryViewModel(
            options,
            previews,
            () => new AppearanceGalleryOptionId(CurrentAppearanceKey()),
            option =>
            {
                var choice = _appearances.FirstOrDefault(candidate => AppearanceKey(candidate) == option.Id.Value);
                return choice != null && ApplyAppearance(choice);
            });

        BehaviorListItemViewModel.Build(BehaviorList, host.Catalog.All);
        Behavior = host.Catalog.Classify(door);
        BuildBasicRows();
        RebuildBehaviorSection();
    }

    [RelayCommand]
    public void ChooseBehavior(IBehaviorDescriptor? descriptor)
    {
        if (descriptor is not DoorBehavior behavior || behavior.Id == Behavior.Id)
            return;

        _ = ChooseBehaviorGuardedAsync(behavior);
    }

    /// <summary>
    /// Observes the command's fire-and-forget switch. A fault would otherwise vanish as an
    /// unobserved task while the rail stayed highlighting a behavior the document never got, so
    /// it is handled the way a declined prompt is: put the highlight back on what the door
    /// actually is.
    /// </summary>
    private async Task ChooseBehaviorGuardedAsync(DoorBehavior behavior)
    {
        try
        {
            await ChooseBehaviorAsync(behavior).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _host.Log?.Write(_host.Texts.Get(
                BehaviorEditorStringId.BehaviorSwitchFailed, behavior.DisplayName, ex.Message));
            BehaviorListItemViewModel.Select(BehaviorList, Behavior.Id);
        }
    }

    /// <summary>
    /// The switch itself, with the confirmation in front of it when something real is being
    /// discarded.
    /// </summary>
    /// <remarks>
    /// A door is classified as the raw behavior whenever it carries locals, and those locals are
    /// frequently unrelated gameplay wiring rather than anything a door behavior owns. Choosing a
    /// preset then swept the whole VarTable with nothing said, and the loss became permanent on
    /// the next save.
    /// </remarks>
    public async Task ChooseBehaviorAsync(DoorBehavior behavior)
    {
        ArgumentNullException.ThrowIfNull(behavior);

        var previous = Behavior;
        if (behavior.Id == previous.Id)
            return;

        // Entering the raw behavior clears nothing: it is the raw editor for these very fields, and
        // nothing is replacing them.
        //
        // A door is deliberately NOT on that rule. A host may classify a door by its locals, so
        // switching such a door to the raw behavior is precisely how a builder unwires it - the
        // clear is the operation, not a side effect of it. What a door needs is the confirmation
        // below, for the locals the preset does not own.
        var losses = BehaviorSwitchLosses.Describe(
            Store,
            previous.Manages,
            previous.Fields,
            behavior.Manages,
            DoorBehaviorValueStore.LocalsClearedBySwitchingFrom(Store, previous));

        if (losses.Count > 0 && _host.Prompts != null)
        {
            var texts = _host.Texts;
            var confirmed = await _host.Prompts.ConfirmDestructiveAsync(
                texts.Get(BehaviorEditorStringId.ChangeBehaviorHeadline, behavior.DisplayName),
                texts.Get(
                    losses.Count == 1
                        ? BehaviorEditorStringId.DoorSwitchClearsOne
                        : BehaviorEditorStringId.DoorSwitchClearsMany,
                    texts.DescribeLosses(losses),
                    behavior.DisplayName),
                texts.Get(BehaviorEditorStringId.ChangeBehaviorConfirm)).ConfigureAwait(true);

            if (!confirmed)
            {
                // Put the rail's highlight back on what the door actually is.
                BehaviorListItemViewModel.Select(BehaviorList, previous.Id);
                return;
            }
        }

        if (!RunEdit(_host.Texts.Get(BehaviorEditorStringId.SetBehavior, behavior.DisplayName), () =>
            {
                Store.Clear(previous);
                Store.Apply(behavior, _isInstance);
            }))
        {
            BehaviorListItemViewModel.Select(BehaviorList, previous.Id);
            return;
        }

        Behavior = behavior;
        RebuildBehaviorSection();
        ReloadRowsFromDocument();
    }

    public void ReloadFromDocument()
    {
        var classified = _host.Catalog.Classify(Store.Door);
        if (classified.Id != Behavior.Id)
        {
            Behavior = classified;
            RebuildBehaviorSection();
        }

        ReloadRowsFromDocument();
        OnPresentationChanged();
    }

    /// <summary>Rebuilds the category row after its module palette changes.</summary>
    public void RefreshPaletteChoices()
    {
        var index = BasicRows
            .Select((row, rowIndex) => (row, rowIndex))
            .Where(item => item.row.Definition.Name == "PaletteID")
            .Select(item => item.rowIndex)
            .DefaultIfEmpty(-1)
            .Single();
        if (index < 0)
            return;

        var definition = BasicRows[index].Definition;
        BasicRows[index].Dispose();
        BasicRows[index] = CreateRow(definition);
        RefreshCompleteness();
    }

    /// <summary>Rebuilds every materialized choice row after TLK-backed labels change.</summary>
    public void RefreshTlkLabels()
    {
        RebuildChoiceRows(BasicRows);
        RebuildChoiceRows(BehaviorRows);
        RefreshCompleteness();
    }

    public void SetDirty(bool value) => IsDirty = value;

    /// <summary>Rebuilds appearance pictures after the host's game resources change.</summary>
    public virtual void ReloadGameResources()
    {
        Appearance.ReloadPreviews();
        OnPresentationChanged();
    }

    /// <summary>
    /// Called after the door's appearance or document changed, so a derived editor can rebuild
    /// anything drawn from it, such as a model preview.
    /// </summary>
    protected virtual void OnPresentationChanged()
    {
    }

    private void RebuildChoiceRows(ObservableCollection<DoorRowViewModel> rows)
    {
        for (var index = 0; index < rows.Count; index++)
        {
            if (rows[index].Definition.ChoicesKey == null)
                continue;

            var definition = rows[index].Definition;
            rows[index].Dispose();
            rows[index] = CreateRow(definition);
        }
    }

    private bool RunEdit(string description, Action mutation)
    {
        var applied = _runEdit(description, mutation);
        if (applied)
            IsDirty = true;
        return applied;
    }

    private void BuildBasicRows()
    {
        foreach (var definition in _host.Catalog.BasicFields)
            BasicRows.Add(CreateRow(definition));
    }

    private void RebuildBehaviorSection()
    {
        foreach (var row in BehaviorRows)
            row.Dispose();

        BehaviorRows.Clear();
        foreach (var definition in Behavior.Fields)
            BehaviorRows.Add(CreateRow(definition));

        Variables = Behavior.AllowsVariables
            ? _host.CreateVariables(RunEdit, Store.Locals)
            : null;

        BehaviorListItemViewModel.Select(BehaviorList, Behavior.Id);

        UpdateConditionalRows();
        OnPropertyChanged(nameof(HeaderName));
        OnPropertyChanged(nameof(ShowsVariablesTab));
        RefreshCompleteness();
    }

    private DoorRowViewModel CreateRow(DoorFieldDefinition definition)
    {
        return new DoorRowViewModel(
            definition,
            Store,
            RunEdit,
            _host.ResolveDestination,
            ApplyDerivedMutation,
            OnRowChanged,
            _host.ChoicesFor(definition),
            _host.KeyItems,
            _host.ChoicePreviews,
            _host.Texts);
    }

    private void ApplyDerivedMutation(DoorFieldDefinition definition)
    {
        if (definition.Name == "Locked" &&
            Store.GetInteger(BehaviorFieldStorage.Field, "Locked") != 1)
        {
            Store.ClearConditionalLockFields(Behavior.Fields);
        }

        if (definition.Name == "KeyName")
            Store.UpdateKeyRequired();
    }

    private void OnRowChanged(DoorRowViewModel row)
    {
        if (row.Definition.Name == "Locked")
        {
            UpdateConditionalRows();
            foreach (var conditional in BehaviorRows.Where(candidate =>
                         candidate.Definition.VisibleWhenField == "Locked"))
            {
                conditional.Reload();
            }
        }

        foreach (var candidate in BasicRows.Concat(BehaviorRows))
            candidate.RefreshStatus();

        OnPropertyChanged(nameof(AppearanceDescription));
        OnPropertyChanged(nameof(DoorTag));
        OnPropertyChanged(nameof(TemplateResRef));
        RefreshCompleteness();
    }

    private void UpdateConditionalRows()
    {
        foreach (var row in BehaviorRows)
        {
            row.IsVisible = row.Definition.VisibleWhenField == null ||
                Store.GetInteger(BehaviorFieldStorage.Field, row.Definition.VisibleWhenField) ==
                row.Definition.VisibleWhenValue;
        }
    }

    private void ReloadRowsFromDocument()
    {
        foreach (var row in BasicRows.Concat(BehaviorRows))
            row.Reload();

        Appearance.ReloadFromDocument();
        UpdateConditionalRows();
        Variables?.RefreshFromDocument();
        foreach (var row in BasicRows.Concat(BehaviorRows))
            row.RefreshStatus();

        OnPropertyChanged(nameof(AppearanceDescription));
        OnPropertyChanged(nameof(DoorTag));
        OnPropertyChanged(nameof(TemplateResRef));
        RefreshCompleteness();
    }

    /// <summary>
    /// Identity for an appearance row. The kind has to be part of it: generic row 12 and
    /// specific row 12 are different doors, and they are stored in different fields.
    /// </summary>
    private static string AppearanceKey(DoorAppearanceChoice choice) =>
        $"{choice.Kind}:{choice.Id}";

    private string CurrentAppearanceKey()
    {
        var stored = DoorAppearanceValueStore.Read(Store);
        var current = _appearances.FirstOrDefault(choice =>
            choice.Kind == stored.Kind && choice.Id == stored.Id);
        return current == null
            ? AppearanceGalleryOptionId.Unknown.Value
            : AppearanceKey(current);
    }

    private bool ApplyAppearance(DoorAppearanceChoice choice)
    {
        if (!RunEdit(
                _host.Texts.Get(BehaviorEditorStringId.ChangeAppearance, choice.Display),
                () => DoorAppearanceValueStore.Write(
                    Store, new DoorAppearanceSelection(choice.Kind, choice.Id))))
            return false;

        OnPresentationChanged();
        OnPropertyChanged(nameof(AppearanceDescription));
        return true;
    }

    private void RefreshCompleteness()
    {
        var missing = BehaviorRows
            .Where(row => row.IsVisible && row.IsRequired && !row.HasValue)
            .Select(row => row.Label)
            .ToList();

        Incomplete = missing.Count == 0
            ? null
            : _host.Texts.Get(BehaviorEditorStringId.StillNeeds, Behavior.DisplayName, string.Join(", ", missing));
        OnPropertyChanged(nameof(Incomplete));
        OnPropertyChanged(nameof(IsIncomplete));
    }

    public virtual void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        foreach (var row in BasicRows.Concat(BehaviorRows))
            row.Dispose();
        Appearance.Dispose();
    }
}
