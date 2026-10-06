using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nwn.Authoring.Behaviors;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Triggers;
using Nwn.Toolset.Avalonia.Behaviors;
using Nwn.Toolset.Avalonia.Variables;

namespace Nwn.Toolset.Avalonia.Triggers;

/// <summary>The behavior-shaped trigger editor shared by blueprints and placements.</summary>
/// <remarks>
/// The Behavior tab is the centre of it: pick what the trigger is for - a transition, a trap, a script
/// volume - and its own fields appear. Swapping behavior clears what the previous one owned before writing
/// what the new one manages, as one undo step. Geometry and placement belong to the area's own form, not
/// to this editor.
/// </remarks>
public partial class TriggerBehaviorEditorViewModel : ObservableObject, IDisposable
{
    private readonly BehaviorValueStore _store;
    private readonly TriggerBehaviorEditorHost _host;
    private readonly Func<string, Action, bool> _runEdit;
    private readonly bool _isInstance;
    private bool _disposed;

    public ObservableCollection<BehaviorListItemViewModel> BehaviorList { get; } = new();

    public ObservableCollection<TriggerRowViewModel> BasicRows { get; } = new();

    public ObservableCollection<TriggerRowViewModel> BehaviorRows { get; } = new();

    /// <summary>The raw local-variable grid. Present only while the behavior is the raw one.</summary>
    [ObservableProperty]
    private VarTableSectionViewModel? _variables;

    [ObservableProperty]
    private TriggerBehavior _behavior;

    /// <summary>The editor's captions.</summary>
    public BehaviorEditorTexts Texts => _host.Texts;

    /// <summary>Header: the behavior's name, which is what the trigger actually is.</summary>
    public string HeaderName => Behavior.DisplayName;

    public string HeaderKind => _host.Texts.Get(_isInstance
        ? BehaviorEditorStringId.KindInstance
        : BehaviorEditorStringId.KindBlueprint);

    /// <summary>Header: the file this trigger lives in - its own resref, or its area's.</summary>
    public string HeaderOwner { get; private set; }

    public void SetHeaderOwner(string value)
    {
        HeaderOwner = value;
        OnPropertyChanged(nameof(HeaderOwner));
    }

    public bool ShowsVariablesTab => Behavior.AllowsVariables;

    /// <summary>Everything the behavior needs but has not been given, for the footer warning.</summary>
    public string? Incomplete { get; private set; }

    public bool IsIncomplete => Incomplete != null;

    /// <summary>The trigger this editor edits.</summary>
    public JsonGffStruct Trigger => _store.ValueStruct;

    /// <param name="trigger">The blueprint root or placed trigger struct.</param>
    /// <param name="headerOwner">The file the trigger lives in.</param>
    /// <param name="isInstance">True for a placement, false for a blueprint.</param>
    /// <param name="runEdit">The host transaction every write runs through.</param>
    /// <param name="host">The host's behaviors, choices and services.</param>
    public TriggerBehaviorEditorViewModel(
        JsonGffStruct trigger,
        string headerOwner,
        bool isInstance,
        Func<string, Action, bool> runEdit,
        TriggerBehaviorEditorHost host)
    {
        ArgumentNullException.ThrowIfNull(trigger);
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _store = new BehaviorValueStore(trigger);
        _runEdit = runEdit ?? throw new ArgumentNullException(nameof(runEdit));
        _isInstance = isInstance;
        _behavior = host.Catalog.Custom;
        HeaderOwner = headerOwner;

        BehaviorListItemViewModel.Build(BehaviorList, host.Catalog.All);
        Behavior = host.Catalog.Classify(trigger);
        BuildBasicRows();
        RebuildBehaviorSection();
    }

    /// <summary>
    /// Switches behavior: clear what the old one owned, then write what the new one manages, as one undo
    /// step so a mis-click is one Ctrl+Z rather than several.
    /// </summary>
    [RelayCommand]
    public void ChooseBehavior(IBehaviorDescriptor? descriptor)
    {
        if (descriptor is not TriggerBehavior behavior || behavior.Id == Behavior.Id)
            return;

        _ = ChooseBehaviorGuardedAsync(behavior);
    }

    /// <summary>
    /// Observes the command's fire-and-forget switch. A fault would otherwise vanish as an unobserved
    /// task while the rail stayed highlighting a behavior the document never got, so it is handled the way
    /// a declined prompt is: put the highlight back on what the trigger actually is.
    /// </summary>
    private async Task ChooseBehaviorGuardedAsync(TriggerBehavior behavior)
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
    /// The switch itself, with the confirmation in front of it when something real is being discarded.
    /// </summary>
    /// <remarks>
    /// The raw behavior is the case that needs this: its fields are every raw script slot the trigger has,
    /// and the clear removes all of them before a preset writes its own. Entering the raw behavior clears
    /// nothing, because it is the raw editor for those very fields and nothing replaces them.
    /// </remarks>
    public async Task ChooseBehaviorAsync(TriggerBehavior behavior)
    {
        ArgumentNullException.ThrowIfNull(behavior);

        var previous = Behavior;
        if (behavior.Id == previous.Id)
            return;

        var entersRawEditing = behavior.AllowsVariables;
        var losses = entersRawEditing
            ? Array.Empty<string>()
            : BehaviorSwitchLosses.Describe(_store, previous.Manages, previous.Fields, behavior.Manages);

        if (losses.Count > 0 && _host.Prompts != null)
        {
            var texts = _host.Texts;
            var confirmed = await _host.Prompts.ConfirmDestructiveAsync(
                texts.Get(BehaviorEditorStringId.ChangeBehaviorHeadline, behavior.DisplayName),
                texts.Get(
                    losses.Count == 1
                        ? BehaviorEditorStringId.TriggerSwitchClearsOne
                        : BehaviorEditorStringId.TriggerSwitchClearsMany,
                    texts.DescribeLosses(losses),
                    behavior.DisplayName),
                texts.Get(BehaviorEditorStringId.ChangeBehaviorConfirm)).ConfigureAwait(true);

            if (!confirmed)
            {
                // Put the rail's highlight back on what the trigger actually is.
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

    /// <summary>
    /// Re-reads everything from the document, after a revert, an undo or redo, or an external reload -
    /// including which behavior the document now describes, so the editor follows what the trigger is
    /// rather than claiming a behavior it no longer has.
    /// </summary>
    public void ReloadFromDocument()
    {
        var classified = _host.Catalog.Classify(_store.ValueStruct);
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

    private void RebuildChoiceRows(ObservableCollection<TriggerRowViewModel> rows)
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

    private void ReloadRowsFromDocument()
    {
        foreach (var row in BasicRows.Concat(BehaviorRows))
            row.Reload();

        Variables?.RefreshFromDocument();
        RefreshCompleteness();
    }

    private void BuildBasicRows()
    {
        foreach (var definition in _host.Catalog.BasicFields)
            BasicRows.Add(CreateRow(definition));
    }

    private TriggerRowViewModel CreateRow(BehaviorFieldDefinition definition) =>
        new(definition, _store, _runEdit, _host.ResolveDestination, _host.Texts, _host.ChoicesFor(definition),
            RefreshAfterValueChange, _host.ChoicePreviews);

    /// <summary>
    /// A destination's status depends on its type row and its tag row, so any edit refreshes every row's.
    /// </summary>
    private void RefreshAfterValueChange()
    {
        RefreshCompleteness();
        foreach (var row in BehaviorRows)
            row.RefreshStatus();
    }

    private void RebuildBehaviorSection()
    {
        // A row can be holding a pending gallery search; dropping it without saying so leaves that timer
        // to fire against a form nobody is looking at.
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

    /// <summary>
    /// Names what the behavior still needs. Stated rather than blocked: a half-configured trigger is a
    /// normal step on the way to a finished one, and refusing to save it helps nobody.
    /// </summary>
    private void RefreshCompleteness()
    {
        UpdateConditionalRows();
        var missing = BehaviorRows
            .Where(row => row.IsVisible && row.IsRequired && row.IsEmpty)
            .Select(row => row.Label)
            .ToList();

        Incomplete = missing.Count == 0
            ? null
            : _host.Texts.Get(BehaviorEditorStringId.StillNeeds, Behavior.DisplayName, string.Join(", ", missing));

        OnPropertyChanged(nameof(Incomplete));
        OnPropertyChanged(nameof(IsIncomplete));
    }

    /// <summary>
    /// Shows a conditional row only while the trigger holds the value that makes it applicable, so a
    /// transition's destination does not clutter a trigger that is not a transition.
    /// </summary>
    private void UpdateConditionalRows()
    {
        foreach (var row in BasicRows.Concat(BehaviorRows))
        {
            if (row.Definition is not TriggerFieldDefinition { VisibleWhenField: { } field } conditional)
                continue;

            row.IsVisible = _store.GetInteger(BehaviorFieldStorage.Field, field) == conditional.VisibleWhenValue;
        }
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
