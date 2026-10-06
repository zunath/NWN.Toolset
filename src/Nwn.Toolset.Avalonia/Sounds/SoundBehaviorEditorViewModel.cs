using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nwn.Authoring.Behaviors;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Sounds;
using Nwn.Toolset.Avalonia.Behaviors;
using Nwn.Toolset.Avalonia.Variables;

namespace Nwn.Toolset.Avalonia.Sounds;

/// <summary>The behavior editor shared by ambient-sound blueprints and area placements.</summary>
public partial class SoundBehaviorEditorViewModel : ObservableObject, IDisposable
{
    private readonly SoundBehaviorValueStore _store;
    private readonly SoundBehaviorEditorHost _host;
    private readonly Func<string, Action, bool> _runEdit;
    private readonly bool _isInstance;
    private bool _disposed;

    public ObservableCollection<BehaviorListItemViewModel> BehaviorList { get; } = new();

    public ObservableCollection<SoundRowViewModel> BasicRows { get; } = new();

    public ObservableCollection<SoundRowViewModel> BehaviorRows { get; } = new();

    [ObservableProperty]
    private VarTableSectionViewModel? _variables;

    [ObservableProperty]
    private SoundBehavior _behavior;

    [ObservableProperty]
    private string? _behaviorChangeNotice;

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

    /// <summary>Raised after any edit this editor applied.</summary>
    public event Action? ValueChanged;

    public SoundBehaviorEditorViewModel(
        JsonGffStruct sound,
        string headerOwner,
        bool isInstance,
        Func<string, Action, bool> runEdit,
        SoundBehaviorEditorHost host)
    {
        ArgumentNullException.ThrowIfNull(sound);
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _store = new SoundBehaviorValueStore(sound);
        _runEdit = runEdit ?? throw new ArgumentNullException(nameof(runEdit));
        _isInstance = isInstance;
        _behavior = host.Catalog.Custom;
        HeaderOwner = headerOwner;

        BehaviorListItemViewModel.Build(BehaviorList, host.Catalog.All);
        Behavior = host.Catalog.Classify(sound);
        BuildBasicRows();
        RebuildBehaviorSection();
    }

    [RelayCommand]
    public void ChooseBehavior(IBehaviorDescriptor? descriptor)
    {
        if (descriptor is not SoundBehavior behavior || behavior.Id == Behavior.Id)
            return;

        _ = ChooseBehaviorGuardedAsync(behavior);
    }

    /// <summary>
    /// Observes the command's fire-and-forget switch. A fault would otherwise vanish as an
    /// unobserved task while the rail stayed highlighting a behavior the document never got, so
    /// it is handled the way a declined prompt is: put the highlight back on what the sound
    /// actually is.
    /// </summary>
    private async Task ChooseBehaviorGuardedAsync(SoundBehavior behavior)
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
    /// discarded — the same flow as the door, trigger and waypoint editors.
    /// </summary>
    public async Task ChooseBehaviorAsync(SoundBehavior behavior)
    {
        ArgumentNullException.ThrowIfNull(behavior);

        var previous = Behavior;
        if (behavior.Id == previous.Id)
            return;

        var texts = _host.Texts;
        var sounds = _store.GetSounds().ToList();
        var keptSounds = behavior.IsLoop ? sounds.Take(1).ToList() : sounds;
        var droppedSounds = behavior.IsLoop ? sounds.Skip(1).ToList() : new List<string>();

        // Entering the raw behavior clears nothing. It is the raw editor for these very fields, so
        // wiping them on the way in leaves the panel that exists to expose the configuration
        // opening with the configuration erased. Nothing is replacing any of it either, which is
        // what makes the clear pure loss rather than a swap.
        var entersRawEditing = behavior.AllowsVariables;

        // Mirrors SoundBehaviorValueStore.Clear: a field the incoming preset also owns as an
        // editable slot is kept rather than cleared, so it must not be named as a loss. Leaving
        // the raw behavior keeps nothing.
        var keptByIncoming = new HashSet<string>(
            behavior.Fields.Select(field => field.Name), StringComparer.Ordinal);
        var losses = entersRawEditing
            ? Array.Empty<string>()
            : BehaviorSwitchLosses.Describe(
                _store,
                previous.Manages,
                previous.AllowsVariables
                    ? previous.Fields
                    : previous.Fields.Where(field => !keptByIncoming.Contains(field.Name)),
                behavior.Manages);

        // Dropped sound entries are as much a loss as cleared fields: a loop switch that
        // truncates the playlist must ask first even when no field is cleared.
        var clauses = new List<string>();
        if (losses.Count > 0)
        {
            clauses.Add(texts.Get(
                losses.Count == 1 ? BehaviorEditorStringId.SoundClearsOne : BehaviorEditorStringId.SoundClearsMany,
                texts.DescribeLosses(losses),
                behavior.DisplayName));
        }
        if (droppedSounds.Count > 0)
            clauses.Add(texts.Get(BehaviorEditorStringId.SoundDropsEntries, texts.DescribeLosses(droppedSounds)));

        if (clauses.Count > 0 && _host.Prompts != null)
        {
            var confirmed = await _host.Prompts.ConfirmDestructiveAsync(
                texts.Get(BehaviorEditorStringId.ChangeBehaviorHeadline, behavior.DisplayName),
                texts.Get(
                    BehaviorEditorStringId.SoundSwitchMessage,
                    string.Join(texts.Get(BehaviorEditorStringId.ClauseJoin), clauses)),
                texts.Get(BehaviorEditorStringId.ChangeBehaviorConfirm)).ConfigureAwait(true);

            if (!confirmed)
            {
                // Put the rail's highlight back on what the sound actually is.
                BehaviorListItemViewModel.Select(BehaviorList, previous.Id);
                return;
            }
        }

        var applied = RunEdit(texts.Get(BehaviorEditorStringId.SetBehavior, behavior.DisplayName), () =>
        {
            if (!entersRawEditing)
                _store.Clear(previous, behavior);

            foreach (var value in behavior.Manages)
                _store.Apply(value, _isInstance);

            _store.ReplaceSounds(keptSounds);
        });
        if (!applied)
        {
            BehaviorListItemViewModel.Select(BehaviorList, previous.Id);
            return;
        }

        Behavior = behavior;
        BehaviorChangeNotice = droppedSounds.Count == 0
            ? null
            : texts.Get(BehaviorEditorStringId.SoundLoopKept, keptSounds[0], string.Join(", ", droppedSounds));
        RebuildBehaviorSection();
        ReloadRowsFromDocument();
    }

    public void ReloadFromDocument()
    {
        BehaviorChangeNotice = null;
        var classified = _host.Catalog.Classify(_store.Sound);
        if (classified.Id != Behavior.Id)
        {
            Behavior = classified;
            RebuildBehaviorSection();
        }

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

    private void RebuildChoiceRows(ObservableCollection<SoundRowViewModel> rows)
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
            ValueChanged?.Invoke();
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
            ? _host.CreateVariables(RunEdit, _store.Locals)
            : null;

        BehaviorListItemViewModel.Select(BehaviorList, Behavior.Id);

        OnPropertyChanged(nameof(HeaderName));
        OnPropertyChanged(nameof(ShowsVariablesTab));
        RefreshCompleteness();
    }

    private SoundRowViewModel CreateRow(BehaviorFieldDefinition definition) =>
        new(
            definition,
            _store,
            RunEdit,
            _host.ChoicesFor(definition),
            _host.AudioResources,
            RefreshCompleteness,
            _host.Preview);

    private void ReloadRowsFromDocument()
    {
        foreach (var row in BasicRows.Concat(BehaviorRows))
            row.Reload();

        Variables?.RefreshFromDocument();
        RefreshCompleteness();
    }

    private void RefreshCompleteness()
    {
        var missing = BehaviorRows
            .Where(row => row.IsRequired && !row.HasValue)
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
    }
}
