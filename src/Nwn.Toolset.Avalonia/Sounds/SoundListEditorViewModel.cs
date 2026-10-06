using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nwn.Authoring.Behaviors;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Formats.Resources;
using Nwn.Toolset.Avalonia.Localization;

namespace Nwn.Toolset.Avalonia.Sounds;

/// <summary>Edits an ordered native GFF list of sound ResRefs through its owning document transaction.</summary>
public sealed partial class SoundListEditorViewModel : ObservableObject
{
    private const int MaxSearchResults = 200;
    private readonly BehaviorValueStore _store;
    private readonly SoundListSchema _schema;
    private readonly Func<string, Action, bool> _runEdit;
    private readonly Action _changed;
    private readonly int _maxItems;
    private readonly SoundListTexts _texts;
    private readonly ISoundListPreview? _preview;
    private int _matchCount;

    public ObservableCollection<SoundListEntryViewModel> Rows { get; } = new();
    public IReadOnlyList<string> AvailableSounds { get; }
    public ObservableCollection<string> FilteredSounds { get; } = new();
    public bool HasAudioCatalog => AvailableSounds.Count > 0;
    public bool HasRoom => _maxItems == 0 || Rows.Count < _maxItems;
    public bool HasValidCount => Rows.Count > 0 && (_maxItems == 0 || Rows.Count <= _maxItems);
    public bool HasSelection => SelectedEntry is not null;
    public bool CanPreview => _preview?.IsAvailable == true;
    public string? PreviewTarget => SelectedEntry?.ResRef ?? Candidate;
    public string NoCatalogLabel => _texts.Get(SoundListStringId.NoCatalog);
    public string SearchWatermark => _texts.Get(SoundListStringId.SearchWatermark);
    public string AddLabel => _texts.Get(SoundListStringId.Add);
    public string PlayLabel => _texts.Get(SoundListStringId.Play);
    public string StopLabel => _texts.Get(SoundListStringId.Stop);
    public string RemoveLabel => _texts.Get(SoundListStringId.Remove);
    public string MoveUpLabel => _texts.Get(SoundListStringId.MoveUp);
    public string MoveDownLabel => _texts.Get(SoundListStringId.MoveDown);
    public string SearchSummary => AvailableSounds.Count == 0
        ? _texts.Get(SoundListStringId.NoSounds)
        : _matchCount == AvailableSounds.Count
            ? _matchCount == 1
                ? _texts.Get(SoundListStringId.SoundCount, _matchCount)
                : _texts.Get(SoundListStringId.SoundsCount, _matchCount)
            : _matchCount == 0
                ? _texts.Get(SoundListStringId.NoMatches)
                : _texts.Get(SoundListStringId.MatchingSoundsCount, _matchCount, AvailableSounds.Count);

    [ObservableProperty]
    private SoundListEntryViewModel? _selectedEntry;

    [ObservableProperty]
    private string? _candidate;

    [ObservableProperty]
    private string _search = string.Empty;

    [ObservableProperty]
    private string? _status;

    public SoundListEditorViewModel(
        BehaviorValueStore store,
        SoundListSchema schema,
        IReadOnlyList<string> availableSounds,
        int maxItems,
        Func<string, Action, bool> runEdit,
        Action changed,
        SoundListTexts? texts = null,
        ISoundListPreview? preview = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _schema = schema ?? throw new ArgumentNullException(nameof(schema));
        if (string.IsNullOrWhiteSpace(_schema.ListFieldName) || string.IsNullOrWhiteSpace(_schema.ResRefFieldName))
            throw new ArgumentException("The native sound-list field names are required.", nameof(schema));
        ArgumentNullException.ThrowIfNull(availableSounds);
        ArgumentNullException.ThrowIfNull(runEdit);
        ArgumentNullException.ThrowIfNull(changed);
        if (maxItems < 0)
            throw new ArgumentOutOfRangeException(nameof(maxItems));
        var soundSnapshot = availableSounds.ToArray();
        AvailableSounds = Array.AsReadOnly(soundSnapshot.Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
        _maxItems = maxItems;
        _runEdit = runEdit;
        _changed = changed;
        _texts = texts ?? SoundListTexts.English;
        _preview = preview;
        RebuildFilteredSounds();
        Reload();
    }

    public void Reload()
    {
        var selectedIndex = SelectedEntry?.Index;
        Rows.Clear();
        var sounds = _store.GetResRefList(_schema.ListFieldName, _schema.ResRefFieldName);
        var nativeEntries = _store.ValueStruct.GetOrNull(_schema.ListFieldName)?.Elements;
        for (var index = 0; index < sounds.Count; index++)
            Rows.Add(new SoundListEntryViewModel(index, sounds[index],
                nativeEntries is not null && index < nativeEntries.Count ? nativeEntries[index] : null));
        SelectedEntry = selectedIndex is { } value && value >= 0 && value < Rows.Count ? Rows[value] : null;
        NotifyState();
    }

    private bool CanAdd()
    {
        var candidate = Candidate?.Trim() ?? string.Empty;
        return HasRoom && ResourceReferenceRules.IsValid(candidate)
            && AvailableSounds.Contains(candidate, StringComparer.OrdinalIgnoreCase);
    }

    [RelayCommand(CanExecute = nameof(CanAdd))]
    private void Add()
    {
        var candidate = Candidate?.Trim() ?? string.Empty;
        if (!CanAdd())
            return;
        var expectedRows = Rows.ToArray();
        if (!RunEdit(_texts.Get(SoundListStringId.AddDescription),
                () => CanAdd() && string.Equals(Candidate?.Trim(), candidate, StringComparison.OrdinalIgnoreCase)
                    && HasSameRows(expectedRows),
                () => _store.AddResRefListEntry(_schema.ListFieldName, _schema.ResRefFieldName, candidate)))
            return;

        Candidate = null;
        RebuildFilteredSounds();
        Reload();
        SelectedEntry = Rows.LastOrDefault();
        _changed();
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void Remove()
    {
        if (!TryGetCurrentSelection(out var selected))
            return;
        var expectedRows = Rows.ToArray();
        var description = _texts.Get(SoundListStringId.RemoveDescription, selected.ResRef);
        if (!RunEdit(description, () => ReferenceEquals(SelectedEntry, selected) && HasSameRows(expectedRows),
                () => _store.RemoveListEntry(_schema.ListFieldName, selected.Index)))
            return;

        var nextIndex = Math.Min(selected.Index, Rows.Count - 2);
        Reload();
        SelectedEntry = nextIndex >= 0 && nextIndex < Rows.Count ? Rows[nextIndex] : null;
        _changed();
    }

    private bool CanMoveUp() => IsCurrentSelection() && SelectedEntry is { Index: > 0 };

    [RelayCommand(CanExecute = nameof(CanMoveUp))]
    private void MoveUp()
    {
        if (!TryGetCurrentSelection(out var selected) || selected.Index == 0)
            return;
        var target = selected.Index - 1;
        var expectedRows = Rows.ToArray();
        if (!RunEdit(_texts.Get(SoundListStringId.MoveUpDescription, selected.ResRef),
                () => ReferenceEquals(SelectedEntry, selected) && HasSameRows(expectedRows),
                () => _store.MoveListEntry(_schema.ListFieldName, selected.Index, target)))
            return;
        Reload();
        SelectedEntry = Rows[target];
        _changed();
    }

    private bool CanMoveDown() => IsCurrentSelection() && SelectedEntry is { } selected && selected.Index < Rows.Count - 1;

    [RelayCommand(CanExecute = nameof(CanMoveDown))]
    private void MoveDown()
    {
        if (!TryGetCurrentSelection(out var selected) || selected.Index >= Rows.Count - 1)
            return;
        var target = selected.Index + 1;
        var expectedRows = Rows.ToArray();
        if (!RunEdit(_texts.Get(SoundListStringId.MoveDownDescription, selected.ResRef),
                () => ReferenceEquals(SelectedEntry, selected) && HasSameRows(expectedRows),
                () => _store.MoveListEntry(_schema.ListFieldName, selected.Index, target)))
            return;
        Reload();
        SelectedEntry = Rows[target];
        _changed();
    }

    private bool CanPlay() => CanPreview && !string.IsNullOrWhiteSpace(PreviewTarget);

    [RelayCommand(CanExecute = nameof(CanPlay))]
    private void Play()
    {
        if (_preview is not null)
            Status = _preview.Play(PreviewTarget);
    }

    [RelayCommand]
    private void StopPlayback() => _preview?.Stop();

    partial void OnCandidateChanged(string? value)
    {
        AddCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(PreviewTarget));
        PlayCommand.NotifyCanExecuteChanged();
        RefreshStatus();
    }

    partial void OnSearchChanged(string value) => RebuildFilteredSounds();
    partial void OnSelectedEntryChanged(SoundListEntryViewModel? value) => NotifyState();

    private bool IsCurrentSelection() => SelectedEntry is { } selected && IsCurrentEntry(selected);

    private bool HasSameRows(IReadOnlyList<SoundListEntryViewModel> expectedRows)
    {
        if (Rows.Count != expectedRows.Count)
            return false;
        var values = _store.GetResRefList(_schema.ListFieldName, _schema.ResRefFieldName);
        var nativeEntries = _store.ValueStruct.GetOrNull(_schema.ListFieldName)?.Elements;
        if (values.Count != expectedRows.Count)
            return false;
        if (nativeEntries is null)
            return expectedRows.Count == 0;
        return nativeEntries.Count == expectedRows.Count
            && expectedRows.Select((row, index) => ReferenceEquals(Rows[index], row)
                && row.Index == index && string.Equals(values[index], row.ResRef, StringComparison.Ordinal)
                && ReferenceEquals(nativeEntries[index], row.NativeEntry)).All(matches => matches);
    }

    private bool RunEdit(string description, Func<bool> canApply, Action mutation)
    {
        var accepting = 1;
        var invoked = 0;
        var applied = 0;
        var transactionThread = Environment.CurrentManagedThreadId;
        bool accepted;
        try
        {
            accepted = _runEdit(description, () =>
            {
                if (Volatile.Read(ref accepting) == 0
                    || Environment.CurrentManagedThreadId != transactionThread
                    || Interlocked.Exchange(ref invoked, 1) != 0
                    || !canApply())
                    return;
                mutation();
                Volatile.Write(ref applied, 1);
            });
        }
        finally
        {
            Volatile.Write(ref accepting, 0);
        }
        return accepted && Volatile.Read(ref applied) == 1;
    }

    private bool TryGetCurrentSelection(out SoundListEntryViewModel selected)
    {
        selected = SelectedEntry!;
        return selected is not null && IsCurrentEntry(selected);
    }

    private bool IsCurrentEntry(SoundListEntryViewModel selected)
    {
        if (selected.Index < 0 || selected.Index >= Rows.Count || !ReferenceEquals(Rows[selected.Index], selected))
            return false;
        var values = _store.GetResRefList(_schema.ListFieldName, _schema.ResRefFieldName);
        var nativeEntries = _store.ValueStruct.GetOrNull(_schema.ListFieldName)?.Elements;
        return selected.Index < values.Count && nativeEntries?.Count == values.Count
            && string.Equals(values[selected.Index], selected.ResRef, StringComparison.Ordinal)
            && ReferenceEquals(nativeEntries[selected.Index], selected.NativeEntry);
    }

    private void RebuildFilteredSounds()
    {
        var picked = Candidate;
        var query = Search.Trim();
        FilteredSounds.Clear();
        _matchCount = 0;
        foreach (var sound in AvailableSounds)
        {
            if (query.Length > 0 && !sound.Contains(query, StringComparison.OrdinalIgnoreCase))
                continue;
            FilteredSounds.Add(sound);
            if (++_matchCount >= MaxSearchResults)
                break;
        }
        if (picked is { Length: > 0 } && !FilteredSounds.Contains(picked, StringComparer.OrdinalIgnoreCase))
            FilteredSounds.Insert(0, picked);
        Candidate = picked;
        OnPropertyChanged(nameof(SearchSummary));
    }

    private void NotifyState()
    {
        RefreshStatus();
        OnPropertyChanged(nameof(HasRoom));
        OnPropertyChanged(nameof(HasValidCount));
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(PreviewTarget));
        AddCommand.NotifyCanExecuteChanged();
        RemoveCommand.NotifyCanExecuteChanged();
        MoveUpCommand.NotifyCanExecuteChanged();
        MoveDownCommand.NotifyCanExecuteChanged();
        PlayCommand.NotifyCanExecuteChanged();
    }

    private void RefreshStatus()
    {
        var candidate = Candidate?.Trim() ?? string.Empty;
        if (candidate.Length > 0
            && AvailableSounds.Contains(candidate, StringComparer.OrdinalIgnoreCase)
            && !ResourceReferenceRules.IsValid(candidate))
        {
            Status = _texts.Get(SoundListStringId.InvalidResRef);
            return;
        }

        Status = HasRoom ? null : _texts.Get(_maxItems == 1
                ? SoundListStringId.NoRoomSingular
                : SoundListStringId.NoRoomPlural,
            _maxItems, _texts.Get(SoundListStringId.RemoveToChoose));
    }
}
