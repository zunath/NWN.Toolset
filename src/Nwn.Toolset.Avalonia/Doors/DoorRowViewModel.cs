using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nwn.Authoring.Behaviors;
using Nwn.Authoring.Doors;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Toolset.Avalonia.Behaviors;

namespace Nwn.Toolset.Avalonia.Doors;

/// <summary>
/// One row of the door editor: the shared behavior row plus the two shapes only a door has - an
/// ordered set of required key items, and the lock/trap consistency messages.
/// </summary>
/// <remarks>
/// The picture gallery the appearance and portrait rows use is the shared row's, not this
/// class's. It arrived here and in the trigger editor separately; keeping two copies is how the
/// two came to page and debounce differently.
/// </remarks>
public sealed partial class DoorRowViewModel : BehaviorRowViewModel
{
    private readonly DoorBehaviorValueStore _store;
    private readonly TransitionDestinationResolver? _resolveDestination;
    private readonly Action<DoorFieldDefinition> _applyDerivedMutation;
    private readonly Action<DoorRowViewModel> _changed;
    private readonly IReadOnlyDictionary<int, string> _knownKeyItems;
    private readonly BehaviorEditorTexts _texts;

    protected override bool SelectsFirstChoiceWhenUnset =>
        Definition.Name != "LinkedToFlags";

    public new DoorFieldDefinition Definition { get; }

    public bool IsMultiChoice => Definition.Kind == BehaviorFieldKind.MultiChoice;

    /// <summary>The statement row prints its own note; the shared note line would repeat it.</summary>
    public override bool HasNote => !IsStatement && base.HasNote;

    public override bool HasValue =>
        IsMultiChoice ? SelectedKeyItems.Count > 0 :
        IsTextEntry || IsParagraph ? !string.IsNullOrWhiteSpace(Text) :
        true;

    /// <summary>Every key item the host declares, searched by the picker below.</summary>
    public IReadOnlyList<DoorKeyItemViewModel> AvailableKeyItems { get; }

    /// <summary>The filtered slice of <see cref="AvailableKeyItems"/> the picker shows.</summary>
    public ObservableCollection<DoorKeyItemViewModel> MatchingKeyItems { get; } = new();

    public ObservableCollection<DoorKeyItemViewModel> SelectedKeyItems { get; } = new();

    public string KeyItemSearchSummary =>
        MatchingKeyItems.Count == AvailableKeyItems.Count
            ? _texts.Get(AvailableKeyItems.Count == 1
                ? BehaviorEditorStringId.KeyItemCountOne
                : BehaviorEditorStringId.KeyItemCountMany, AvailableKeyItems.Count)
            : MatchingKeyItems.Count == 0
                ? _texts.Get(BehaviorEditorStringId.NoMatchingKeyItems)
                : _texts.Get(BehaviorEditorStringId.KeyItemMatches, MatchingKeyItems.Count, AvailableKeyItems.Count);

    public string KeyItemSearchWatermark => _texts.KeyItemSearchWatermark;

    [ObservableProperty]
    private string _keyItemSearchText = string.Empty;

    public DoorRowViewModel(
        DoorFieldDefinition definition,
        DoorBehaviorValueStore store,
        Func<string, Action, bool> runEdit,
        TransitionDestinationResolver? resolveDestination,
        Action<DoorFieldDefinition> applyDerivedMutation,
        Action<DoorRowViewModel> changed,
        IReadOnlyList<BehaviorChoice>? choices = null,
        IReadOnlyDictionary<int, string>? keyItems = null,
        IBehaviorChoicePreviewProvider? previews = null,
        BehaviorEditorTexts? texts = null)
        : base(definition, store, runEdit, choices, valueChanged: null, previews)
    {
        Definition = definition;
        _store = store;
        _resolveDestination = resolveDestination;
        _applyDerivedMutation = applyDerivedMutation;
        _changed = changed;
        _texts = texts ?? BehaviorEditorTexts.English;
        _knownKeyItems = keyItems ?? new Dictionary<int, string>();

        AvailableKeyItems = _knownKeyItems
            .Where(entry => entry.Key != 0)
            .OrderBy(entry => entry.Value, StringComparer.OrdinalIgnoreCase)
            .Select(entry => new DoorKeyItemViewModel(
                entry.Key, _texts.Get(BehaviorEditorStringId.KeyItemDisplay, entry.Value, entry.Key), true))
            .ToList();

        Reload();
        RebuildMatchingKeyItems();
    }

    protected override void ReadValue()
    {
        switch (Definition.Special)
        {
            case DoorFieldSpecial.SelfClosing:
                IsChecked = _store.IsSelfClosing;
                return;
            case DoorFieldSpecial.KeyItemSequence:
                ReloadKeyItems();
                return;
            default:
                base.ReadValue();
                return;
        }
    }

    protected override void WriteText(string value)
    {
        base.WriteText(value);

        if (Definition.NonEmptySetsField != null)
        {
            _store.SetInteger(
                BehaviorFieldStorage.Field,
                Definition.NonEmptySetsField,
                GffFieldType.Byte,
                string.IsNullOrWhiteSpace(value) ? 0 : 1);
        }

        _applyDerivedMutation(Definition);
    }

    protected override void WriteNumber(decimal value)
    {
        base.WriteNumber(value);
        _applyDerivedMutation(Definition);
    }

    protected override void WriteCheck(bool value)
    {
        if (Definition.Special == DoorFieldSpecial.SelfClosing)
            _store.SetSelfClosing(value);
        else
            base.WriteCheck(value);

        _applyDerivedMutation(Definition);
    }

    protected override void WriteChoice(BehaviorChoiceViewModel value)
    {
        base.WriteChoice(value);
        _applyDerivedMutation(Definition);
    }

    protected override void OnApplied()
    {
        base.OnApplied();
        NotifyValueShapeChanged();
        _changed(this);
    }

    /// <summary>
    /// Everything the door editor can say about one row: whether its tag resolves, whether a
    /// transition names a destination type, and whether its key items are real.
    /// </summary>
    public override void RefreshStatus()
    {
        var messages = new List<string>();
        var good = true;

        if (Definition.Kind == BehaviorFieldKind.TagReference)
        {
            if (!string.IsNullOrWhiteSpace(Text))
            {
                var scope = Definition.Name == TransitionDestinationFlags.LinkedToField
                    ? TransitionDestinationFlags.ScopeOf(_store.GetInteger(
                        BehaviorFieldStorage.Field, TransitionDestinationFlags.LinkedToFlagsField))
                    : Definition.TagScope;

                var destination = TransitionDestinationDescriber.Resolve(scope, Text, _resolveDestination);
                messages.Add(TransitionDestinationDescriber.Describe(destination, scope, _texts));
                good = destination.IsGood;
            }
            else if (Definition.TagScope == BehaviorTagScope.Item &&
                     _store.GetInteger(BehaviorFieldStorage.Field, "KeyRequired") == 1)
            {
                good = false;
                messages.Add(_texts.Get(BehaviorEditorStringId.KeyRequiredWithoutTag));
            }
        }

        if (IsMultiChoice)
        {
            if (SelectedKeyItems.Count == 0)
            {
                good = false;
                messages.Add(_texts.Get(BehaviorEditorStringId.ChooseKeyItem));
            }
            else
            {
                var invalid = SelectedKeyItems.Where(item => !item.IsValid).Select(item => item.Id).ToList();
                if (invalid.Count > 0)
                {
                    good = false;
                    messages.Add(_texts.Get(
                        invalid.Count == 1
                            ? BehaviorEditorStringId.InvalidKeyItemOne
                            : BehaviorEditorStringId.InvalidKeyItemMany,
                        string.Join(", ", invalid)));
                }
            }
        }

        Status = messages.Count == 0
            ? null
            : string.Join(_texts.Get(BehaviorEditorStringId.StatusSeparator), messages);
        IsStatusGood = good;
    }

    partial void OnKeyItemSearchTextChanged(string value) => RebuildMatchingKeyItems();

    [RelayCommand]
    private void AddKeyItem(DoorKeyItemViewModel? item)
    {
        if (item == null || SelectedKeyItems.Any(selected => selected.Id == item.Id))
            return;

        var ids = SelectedKeyItems.Select(selected => selected.Id).Append(item.Id).ToList();
        if (!RunEditFunc(_texts.Get(BehaviorEditorStringId.AddKeyItem), () => _store.SetRequiredKeyItemIds(ids)))
            return;

        Reload();
        OnApplied();
    }

    [RelayCommand]
    private void RemoveKeyItem(DoorKeyItemViewModel? item)
    {
        if (item == null)
            return;

        var removed = false;
        var ids = new List<int>();
        foreach (var selected in SelectedKeyItems)
        {
            if (!removed && selected.Id == item.Id)
            {
                removed = true;
                continue;
            }

            ids.Add(selected.Id);
        }

        if (!RunEditFunc(_texts.Get(BehaviorEditorStringId.RemoveKeyItem), () => _store.SetRequiredKeyItemIds(ids)))
            return;

        Reload();
        OnApplied();
    }

    private void ReloadKeyItems()
    {
        SelectedKeyItems.Clear();
        foreach (var id in _store.GetRequiredKeyItemIds())
        {
            var display = "";
            var known = id != 0 && _knownKeyItems.TryGetValue(id, out display);
            SelectedKeyItems.Add(new DoorKeyItemViewModel(
                id,
                known
                    ? _texts.Get(BehaviorEditorStringId.KeyItemDisplay, display, id)
                    : _texts.Get(BehaviorEditorStringId.UnknownKeyItem, id),
                known));
        }
    }

    /// <summary>
    /// Republishes the key items matching the search box. Bounded like every other searchable
    /// row: hundreds of key items is a set a builder searches rather than scrolls.
    /// </summary>
    private void RebuildMatchingKeyItems()
    {
        if (!IsMultiChoice)
            return;

        var query = KeyItemSearchText.Trim();
        MatchingKeyItems.Clear();

        var published = 0;
        foreach (var item in AvailableKeyItems)
        {
            if (query.Length > 0 &&
                !item.Display.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            MatchingKeyItems.Add(item);
            if (++published >= MaxSearchResults)
                break;
        }

        OnPropertyChanged(nameof(KeyItemSearchSummary));
    }
}
