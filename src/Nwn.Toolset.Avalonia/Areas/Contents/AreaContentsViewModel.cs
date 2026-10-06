using System.Collections.ObjectModel;
using System.Numerics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nwn.Authoring.Resources;
using Nwn.Toolset.Avalonia.Localization;

namespace Nwn.Toolset.Avalonia.Areas.Contents;

/// <summary>Reusable grouped, filtered tree for the active area's placed instances.</summary>
public partial class AreaContentsViewModel : ObservableObject
{
    private const int MaxRealizedGroupMembers = 200;
    private readonly AreaContentsTexts _texts;
    private readonly List<AreaContentsNodeViewModel> _roots = new();
    private AreaContentsSnapshot? _snapshot;
    private IAreaContentsActions? _actions;
    private AreaContentsIdentity? _forcedVisible;
    private AreaContentsNodeViewModel? _pendingRowReveal;
    private bool _syncingSelection;

    public event Action? RowRevealRequested;
    public ObservableCollection<AreaContentsNodeViewModel> Rows { get; } = new();
    public IReadOnlyList<AreaContentsGroupingOption> GroupingOptions { get; }

    [ObservableProperty]
    private AreaContentsNodeViewModel? _selectedRow;

    [ObservableProperty]
    private string _filter = string.Empty;

    [ObservableProperty]
    private AreaContentsGroupingOption? _selectedGrouping;

    [ObservableProperty]
    private string _statusMessage;

    [ObservableProperty]
    private string _areaResRef = string.Empty;

    public bool HasArea => _snapshot is not null;
    public string SearchWatermark => _texts.Get(AreaContentsStringId.SearchWatermark);
    public string GroupByLabel => _texts.Get(AreaContentsStringId.GroupBy);

    public AreaContentsViewModel(AreaContentsTexts? texts = null)
    {
        _texts = texts ?? AreaContentsTexts.English;
        GroupingOptions = new[]
        {
            new AreaContentsGroupingOption(
                AreaContentsGrouping.Name,
                _texts.Get(AreaContentsStringId.Name),
                _texts.Get(AreaContentsStringId.NameDescription)),
            new AreaContentsGroupingOption(
                AreaContentsGrouping.Blueprint,
                _texts.Get(AreaContentsStringId.Blueprint),
                _texts.Get(AreaContentsStringId.BlueprintDescription)),
            new AreaContentsGroupingOption(
                AreaContentsGrouping.Tag,
                _texts.Get(AreaContentsStringId.Tag),
                _texts.Get(AreaContentsStringId.TagDescription)),
            new AreaContentsGroupingOption(
                AreaContentsGrouping.Flat,
                _texts.Get(AreaContentsStringId.NoGrouping),
                _texts.Get(AreaContentsStringId.NoGroupingDescription))
        };
        _selectedGrouping = GroupingOptions[0];
        _statusMessage = _texts.Get(AreaContentsStringId.NoArea);
    }

    public void SetContents(AreaContentsSnapshot? snapshot, IAreaContentsActions? actions)
    {
        var areaChanged = !string.Equals(
            _snapshot?.AreaResRef,
            snapshot?.AreaResRef,
            StringComparison.OrdinalIgnoreCase);
        _snapshot = snapshot;
        _actions = actions;
        AreaResRef = snapshot?.AreaResRef ?? string.Empty;

        if (areaChanged)
        {
            _forcedVisible = null;
            _pendingRowReveal = null;
            SelectedRow = null;
        }

        OnPropertyChanged(nameof(HasArea));
        Rebuild();
    }

    public void Reveal(AreaContentsIdentity identity)
    {
        _forcedVisible = identity;
        _syncingSelection = true;

        try
        {
            if (!string.IsNullOrWhiteSpace(Filter))
                Filter = string.Empty;
            else
                Rebuild();

            var path = new List<AreaContentsNodeViewModel>();
            var found = _roots.Any(root => TryFindPath(
                root,
                node => node.Kind == AreaContentsNodeKind.Instance && node.Identities.Contains(identity),
                path));
            if (!found || path.Count == 0)
                return;

            foreach (var ancestor in path.Take(path.Count - 1))
                ancestor.IsExpanded = true;
            PublishVisibleRows();
            SelectedRow = path[^1];
            _pendingRowReveal = SelectedRow;
        }
        finally
        {
            _syncingSelection = false;
        }

        RowRevealRequested?.Invoke();
    }

    public bool TryTakePendingRowReveal(out AreaContentsNodeViewModel row)
    {
        if (_pendingRowReveal is null)
        {
            row = null!;
            return false;
        }

        row = _pendingRowReveal;
        _pendingRowReveal = null;
        return true;
    }

    partial void OnFilterChanged(string value) => Rebuild();

    partial void OnSelectedGroupingChanged(AreaContentsGroupingOption? value) => Rebuild();

    private AreaContentsGrouping Grouping => SelectedGrouping?.Value ?? AreaContentsGrouping.Name;
    private bool HasFilter => !string.IsNullOrWhiteSpace(Filter);

    private void Rebuild()
    {
        var expanded = _roots
            .SelectMany(Flatten)
            .Where(node => node.IsExpanded)
            .Select(NodeKey)
            .ToHashSet(StringComparer.Ordinal);
        var reselect = SelectedRow is null ? null : NodeKey(SelectedRow);
        _roots.Clear();

        if (_snapshot is null)
        {
            PublishVisibleRows();
            StatusMessage = _texts.Get(AreaContentsStringId.NoArea);
            return;
        }

        var matched = 0;
        var total = 0;
        foreach (var section in _snapshot.Sections)
        {
            total += section.Entries.Count;
            var entries = Matching(section).ToList();
            matched += entries.Count;
            _roots.Add(BuildKindNode(section, entries));
        }

        foreach (var node in _roots.SelectMany(Flatten))
        {
            node.IsExpanded = expanded.Count == 0
                ? node.Kind == AreaContentsNodeKind.Kind
                : expanded.Contains(NodeKey(node));
        }

        PublishVisibleRows();
        if (reselect is not null)
            SelectedRow = Rows.FirstOrDefault(row => NodeKey(row) == reselect);

        StatusMessage = HasFilter
            ? _texts.Get(AreaContentsStringId.StatusMatches, AreaResRef, matched, total, Filter.Trim())
            : _texts.Get(AreaContentsStringId.StatusObjects, AreaResRef, total);
    }

    private IEnumerable<AreaContentsEntry> Matching(AreaContentsSection section)
    {
        if (!HasFilter)
            return section.Entries;

        var needle = Filter.Trim();
        return section.Entries.Where(entry =>
            Contains(entry.Name, needle) ||
            Contains(entry.TemplateResRef, needle) ||
            Contains(entry.Tag, needle));
    }

    private static bool Contains(string? value, string needle) =>
        value?.Contains(needle, StringComparison.OrdinalIgnoreCase) == true;

    private AreaContentsNodeViewModel BuildKindNode(
        AreaContentsSection section,
        IReadOnlyList<AreaContentsEntry> entries)
    {
        var node = new AreaContentsNodeViewModel(
            AreaContentsNodeKind.Kind,
            section.ResourceType,
            section.Title,
            0,
            _texts.Get(AreaContentsStringId.OpenProperties),
            _texts.Get(AreaContentsStringId.OpenFirstProperties))
        {
            Detail = KindDetail(section, entries)
        };

        if (Grouping == AreaContentsGrouping.Flat)
        {
            var realized = RealizedRows(section.ResourceType, entries);
            foreach (var entry in realized)
                node.Children.Add(BuildInstanceNode(entry, 1, leadWithName: true));
            if (entries.Count > realized.Count)
                node.Children.Add(BuildOverflowNode(section.ResourceType, entries.Count - realized.Count, 1));
            return node;
        }

        var groups = entries
            .GroupBy(GroupKey, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase);
        foreach (var group in groups)
        {
            var members = group.ToList();
            if (members.Count == 1)
                node.Children.Add(BuildInstanceNode(members[0], 1, leadWithName: true));
            else
                node.Children.Add(BuildGroupNode(section.ResourceType, group.Key, members));
        }

        return node;
    }

    private AreaContentsNodeViewModel BuildGroupNode(
        ModuleResourceType type,
        string label,
        IReadOnlyList<AreaContentsEntry> members)
    {
        var node = new AreaContentsNodeViewModel(
            AreaContentsNodeKind.Group,
            type,
            label,
            1,
            _texts.Get(AreaContentsStringId.OpenProperties),
            _texts.Get(AreaContentsStringId.OpenFirstProperties))
        {
            Detail = _texts.Get(AreaContentsStringId.GroupCount, members.Count),
            Indices = members.Select(entry => entry.InstanceIndex).ToArray(),
            Identities = members.Select(entry => entry.Identity).ToArray()
        };
        var realized = RealizedRows(type, members);
        foreach (var entry in realized)
            node.Children.Add(BuildInstanceNode(entry, 2, leadWithName: false));
        if (members.Count > realized.Count)
            node.Children.Add(BuildOverflowNode(type, members.Count - realized.Count, 2));
        return node;
    }

    private IReadOnlyList<AreaContentsEntry> RealizedRows(
        ModuleResourceType type,
        IReadOnlyList<AreaContentsEntry> entries)
    {
        var realized = entries.Take(MaxRealizedGroupMembers).ToList();
        if (_forcedVisible is not { } forced ||
            forced.ResourceType != type ||
            realized.Any(entry => entry.Identity == forced))
            return realized;

        var target = entries.FirstOrDefault(entry => entry.Identity == forced);
        if (target is not null)
            realized.Add(target);
        return realized;
    }

    private AreaContentsNodeViewModel BuildInstanceNode(
        AreaContentsEntry entry,
        int depth,
        bool leadWithName)
    {
        var position = $"{entry.Position.X:0.0}, {entry.Position.Y:0.0}";
        return new AreaContentsNodeViewModel(
            AreaContentsNodeKind.Instance,
            entry.ResourceType,
            leadWithName ? entry.Name : position,
            depth,
            _texts.Get(AreaContentsStringId.OpenProperties),
            _texts.Get(AreaContentsStringId.OpenFirstProperties))
        {
            Detail = leadWithName ? position : entry.TemplateResRef,
            Indices = new[] { entry.InstanceIndex },
            Identities = new[] { entry.Identity },
            Position = entry.Position
        };
    }

    private AreaContentsNodeViewModel BuildOverflowNode(ModuleResourceType type, int count, int depth) =>
        new(
            AreaContentsNodeKind.Overflow,
            type,
            _texts.Get(AreaContentsStringId.Overflow, count),
            depth,
            _texts.Get(AreaContentsStringId.OpenProperties),
            _texts.Get(AreaContentsStringId.OpenFirstProperties));

    private string KindDetail(AreaContentsSection section, IReadOnlyList<AreaContentsEntry> entries)
    {
        var total = section.Entries.Count;
        if (HasFilter)
            return _texts.Get(AreaContentsStringId.KindMatches, entries.Count, total);
        if (Grouping == AreaContentsGrouping.Flat || total == 0)
            return _texts.Get(AreaContentsStringId.KindCount, total);

        var groups = entries.Select(GroupKey).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        return _texts.Get(AreaContentsStringId.KindGroups, groups, total);
    }

    private string GroupKey(AreaContentsEntry entry) => Grouping switch
    {
        AreaContentsGrouping.Blueprint => string.IsNullOrWhiteSpace(entry.TemplateResRef)
            ? _texts.Get(AreaContentsStringId.NoBlueprint)
            : entry.TemplateResRef,
        AreaContentsGrouping.Tag => string.IsNullOrWhiteSpace(entry.Tag)
            ? _texts.Get(AreaContentsStringId.NoTag)
            : entry.Tag,
        _ => entry.Name
    };

    private static string NodeKey(AreaContentsNodeViewModel node) =>
        $"{node.ResourceType}|{node.Kind}|{node.Depth}|{node.Name}|{node.Detail}";

    private static IEnumerable<AreaContentsNodeViewModel> Flatten(AreaContentsNodeViewModel node) =>
        new[] { node }.Concat(node.Children.SelectMany(Flatten));

    private void PublishVisibleRows()
    {
        Rows.Clear();
        foreach (var root in _roots)
            Publish(root);
    }

    private void Publish(AreaContentsNodeViewModel node)
    {
        Rows.Add(node);
        if (!node.IsExpanded)
            return;
        foreach (var child in node.Children)
            Publish(child);
    }

    private static bool TryFindPath(
        AreaContentsNodeViewModel node,
        Func<AreaContentsNodeViewModel, bool> predicate,
        List<AreaContentsNodeViewModel> path)
    {
        path.Add(node);
        if (predicate(node))
            return true;
        foreach (var child in node.Children)
        {
            if (TryFindPath(child, predicate, path))
                return true;
        }
        path.RemoveAt(path.Count - 1);
        return false;
    }

    [RelayCommand]
    private void Toggle(AreaContentsNodeViewModel? node)
    {
        if (node is null || node.Children.Count == 0)
            return;
        node.IsExpanded = !node.IsExpanded;
        PublishVisibleRows();
    }

    partial void OnSelectedRowChanged(AreaContentsNodeViewModel? value)
    {
        if (_syncingSelection || value is not { Kind: AreaContentsNodeKind.Instance, Identities.Count: > 0 })
            return;
        _actions?.Select(value.Identities[0]);
    }

    [RelayCommand]
    private void Open(AreaContentsNodeViewModel? node)
    {
        node ??= SelectedRow;
        if (node is null)
            return;
        if (node.Kind != AreaContentsNodeKind.Instance)
        {
            Toggle(node);
            return;
        }
        if (node.Identities.Count > 0)
            _actions?.Frame(node.Identities[0]);
    }

    [RelayCommand]
    private void OpenProperties(AreaContentsNodeViewModel? node)
    {
        node ??= SelectedRow;
        if (node is { CanOpenProperties: true } && node.Identities.Count > 0)
            _actions?.OpenProperties(node.Identities[0]);
    }

    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        if (SelectedRow is not { IsDeletable: true } node || _actions is null)
            return;
        var requested = node;
        var deleted = await _actions.DeleteAsync(node.Identities, node.Name).ConfigureAwait(true);
        if (deleted && ReferenceEquals(SelectedRow, requested))
            SelectedRow = null;
    }
}
