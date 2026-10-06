using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nwn.Authoring.Behaviors;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Waypoints;
using Nwn.Toolset.Avalonia.Behaviors;
using Nwn.Toolset.Avalonia.Variables;

namespace Nwn.Toolset.Avalonia.Waypoints;

/// <summary>The behavior-shaped waypoint editor shared by blueprints and placements.</summary>
public partial class WaypointBehaviorEditorViewModel : ObservableObject, IDisposable
{
    private readonly BehaviorValueStore _store;
    private readonly WaypointBehaviorEditorHost _host;
    private IWaypointBehaviorCatalog _catalog;
    private readonly Func<string, Action, bool> _runEdit;
    private readonly Func<string, bool>? _singletonTagInUse;
    private readonly bool _isInstance;
    private bool _disposed;

    public ObservableCollection<BehaviorListItemViewModel> BehaviorList { get; } = new();
    public ObservableCollection<WaypointRowViewModel> BasicRows { get; } = new();
    public ObservableCollection<WaypointRowViewModel> BehaviorRows { get; } = new();

    [ObservableProperty]
    private VarTableSectionViewModel? _variables;

    [ObservableProperty]
    private WaypointBehavior _behavior;

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

    public bool NeedsSaveNormalization =>
        Behavior.Manages.Any(value => !_store.Matches(value, _isInstance)) ||
        !HasExpectedPersistedBehavior();

    public string? Incomplete { get; private set; }

    public bool IsIncomplete => Incomplete != null;

    /// <param name="waypoint">The blueprint root or placed waypoint struct.</param>
    /// <param name="headerOwner">The file the waypoint lives in.</param>
    /// <param name="isInstance">True for a placement, false for a blueprint.</param>
    /// <param name="runEdit">The host transaction every write runs through.</param>
    /// <param name="host">The host's behaviors, choices and services.</param>
    /// <param name="singletonTagInUse">
    /// True when a singleton destination tag is already carried by another placed waypoint.
    /// </param>
    public WaypointBehaviorEditorViewModel(
        JsonGffStruct waypoint,
        string headerOwner,
        bool isInstance,
        Func<string, Action, bool> runEdit,
        WaypointBehaviorEditorHost host,
        Func<string, bool>? singletonTagInUse = null)
    {
        ArgumentNullException.ThrowIfNull(waypoint);
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _catalog = host.Catalog ?? throw new ArgumentNullException(nameof(host));
        _store = new BehaviorValueStore(waypoint);
        _runEdit = runEdit ?? throw new ArgumentNullException(nameof(runEdit));
        _singletonTagInUse = singletonTagInUse;
        _isInstance = isInstance;
        HeaderOwner = headerOwner;
        _behavior = _catalog.Classify(waypoint);

        BehaviorListItemViewModel.Build(BehaviorList, _catalog.All);
        BuildBasicRows();
        RebuildBehaviorSection();
    }

    [RelayCommand]
    public void ChooseBehavior(IBehaviorDescriptor? descriptor)
    {
        if (descriptor is not WaypointBehavior behavior || behavior.Id == Behavior.Id)
            return;

        _ = ChooseBehaviorGuardedAsync(behavior);
    }

    /// <summary>
    /// Observes the command's fire-and-forget switch. A fault would otherwise vanish as an
    /// unobserved task while the rail stayed highlighting a behavior the document never got, so
    /// it is handled the way a declined prompt is: put the highlight back on what the waypoint
    /// actually is.
    /// </summary>
    private async Task ChooseBehaviorGuardedAsync(WaypointBehavior behavior)
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
    /// Switches behavior after confirming any custom fields the incoming preset would clear.
    /// </summary>
    public async Task ChooseBehaviorAsync(WaypointBehavior behavior)
    {
        ArgumentNullException.ThrowIfNull(behavior);

        var previous = Behavior;
        if (behavior.Id == previous.Id)
            return;

        // Entering the raw behavior clears nothing. It is the raw editor for these very fields, so
        // wiping them on the way in leaves the panel that exists to expose the configuration
        // opening with the configuration erased. Nothing is replacing any of it either, which is
        // what makes the clear pure loss rather than a swap.
        var entersRawEditing = behavior.AllowsVariables;

        var losses = entersRawEditing
            ? Array.Empty<string>()
            : BehaviorSwitchLosses.Describe(
                _store, previous.Manages, previous.Fields, behavior.Manages);

        if (losses.Count > 0 && _host.Prompts != null)
        {
            var texts = _host.Texts;
            var confirmed = await _host.Prompts.ConfirmDestructiveAsync(
                texts.Get(BehaviorEditorStringId.ChangeBehaviorHeadline, behavior.DisplayName),
                texts.Get(
                    losses.Count == 1
                        ? BehaviorEditorStringId.WaypointSwitchClearsOne
                        : BehaviorEditorStringId.WaypointSwitchClearsMany,
                    texts.DescribeLosses(losses),
                    behavior.DisplayName),
                texts.Get(BehaviorEditorStringId.ChangeBehaviorConfirm)).ConfigureAwait(true);

            if (!confirmed)
            {
                BehaviorListItemViewModel.Select(BehaviorList, previous.Id);
                return;
            }
        }

        var applied = _runEdit(_host.Texts.Get(BehaviorEditorStringId.SetBehavior, behavior.DisplayName), () =>
        {
            if (!entersRawEditing)
                _store.Clear(previous.Manages, previous.Fields);

            foreach (var value in behavior.Manages)
                _store.Apply(value, _isInstance);

            PersistBehavior(behavior);
        });

        if (!applied)
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
        var classified = _catalog.Classify(_store.ValueStruct);
        if (classified.Id != Behavior.Id)
        {
            Behavior = classified;
            RebuildBehaviorSection();
        }

        ReloadRowsFromDocument();
    }

    /// <summary>
    /// Replaces the module-derived classifier and immediately reclassifies the live waypoint
    /// without editing it. A host's classification can depend on other documents - an inbound
    /// transition, say - so retaining the catalog captured at open time can leave a waypoint
    /// displayed as a behavior it no longer plays.
    /// </summary>
    public void RefreshCatalog(IWaypointBehaviorCatalog catalog)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        BehaviorListItemViewModel.Build(BehaviorList, _catalog.All);
        Behavior = _catalog.Classify(_store.ValueStruct);
        RebuildBehaviorSection();
        ReloadRowsFromDocument();
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

    private void RebuildChoiceRows(ObservableCollection<WaypointRowViewModel> rows)
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

    public bool PrepareForSave()
    {
        RefreshCompleteness();
        if (HasSingletonTagConflict())
            return false;

        if (!NeedsSaveNormalization)
            return true;

        var applied = _runEdit(
            _host.Texts.Get(BehaviorEditorStringId.NormalizeWaypointBehavior, Behavior.DisplayName),
            () =>
            {
                foreach (var value in Behavior.Manages)
                    _store.Apply(value, _isInstance);

                PersistBehavior(Behavior);
            });
        if (applied)
            ReloadRowsFromDocument();

        return applied;
    }

    private bool HasExpectedPersistedBehavior()
    {
        if (_catalog.PersistedBehaviorLocal is not { } local)
            return true;

        var persisted = _store.Locals.GetString(local);
        return string.Equals(persisted, Behavior.PersistedId, StringComparison.Ordinal);
    }

    private void PersistBehavior(WaypointBehavior behavior)
    {
        if (_catalog.PersistedBehaviorLocal is not { } local)
            return;

        if (behavior.PersistedId is { } persistedId)
            _store.Locals.SetString(local, persistedId);
        else
            _store.Locals.Remove(local);
    }

    private void ReloadRowsFromDocument()
    {
        foreach (var row in BasicRows.Concat(BehaviorRows))
            row.Reload();

        Variables?.RefreshFromDocument();
        RefreshCompleteness();
    }

    private void BuildBasicRows()
    {
        foreach (var definition in _catalog.BasicFields)
            BasicRows.Add(CreateRow(definition));
    }

    private WaypointRowViewModel CreateRow(BehaviorFieldDefinition definition) =>
        new(definition, _store, _runEdit, _host.ChoicesFor(definition), RefreshCompleteness,
            _host.ChoicePreviews);

    private void RebuildBehaviorSection()
    {
        foreach (var row in BehaviorRows)
            row.Dispose();

        BehaviorRows.Clear();
        foreach (var definition in Behavior.Fields)
            BehaviorRows.Add(CreateRow(definition));

        Variables = Behavior.AllowsVariables
            ? _host.CreateVariables(_runEdit, _store.Locals)
            : null;

        BehaviorListItemViewModel.Select(BehaviorList, Behavior.Id);

        OnPropertyChanged(nameof(HeaderName));
        OnPropertyChanged(nameof(ShowsVariablesTab));
        RefreshCompleteness();
    }

    private void RefreshCompleteness()
    {
        var missing = BehaviorRows
            .Where(row => row.IsRequired && row.IsEmpty)
            .Select(row => row.Label)
            .ToList();

        Incomplete = HasSingletonTagConflict()
            ? _host.Texts.Get(BehaviorEditorStringId.WaypointTagConflict)
            : missing.Count == 0
                ? null
                : _host.Texts.Get(BehaviorEditorStringId.StillNeeds, Behavior.DisplayName, string.Join(", ", missing));

        OnPropertyChanged(nameof(Incomplete));
        OnPropertyChanged(nameof(IsIncomplete));
    }

    private bool HasSingletonTagConflict()
    {
        var tag = _store.GetString(BehaviorFieldStorage.Field, "Tag");
        return _catalog.IsSingletonDestinationTag(tag) &&
               _singletonTagInUse?.Invoke(tag) == true;
    }

    public virtual void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        foreach (var row in BasicRows.Concat(BehaviorRows))
            row.Dispose();
    }
}
