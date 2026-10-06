using Nwn.Authoring.Categories;
using Nwn.Authoring.Resources;

namespace Nwn.Toolset.Avalonia.Explorer.Workflow;

/// <summary>
/// Projects one category section onto Module Contents nodes: folders (pinned first, then alphabetical),
/// their members, and the synthetic Unsorted bucket. Folder counts are what the folder and everything
/// beneath it will actually show, so a filter never leaves a count promising rows it removed.
/// </summary>
internal sealed class ExplorerTreeBuilder
{
    private readonly ModuleResourceType _type;
    private readonly CategorySection _section;
    private readonly IModuleExplorerOrganization? _organization;
    private readonly string _unsortedLabel;

    public ExplorerTreeBuilder(
        ModuleResourceType type,
        CategorySection section,
        IModuleExplorerOrganization? organization,
        string unsortedLabel)
    {
        _type = type;
        _section = section;
        _organization = organization;
        _unsortedLabel = unsortedLabel;
    }

    public IEnumerable<ExplorerNodeViewModel> Roots(IReadOnlyDictionary<string, ExplorerItem> items)
    {
        foreach (var folder in Ordered(_section.Folders))
            yield return Folder(folder, items, depth: 0);

        var unsorted = _section
            .UnsortedResRefs(items.Keys)
            .Select(resRef => items[resRef])
            .ToList();

        // Unsorted is also the drag target that takes a resource back out of a folder. Kept visible when
        // empty; otherwise a completely filed section has no drag-and-drop route out of its folders and
        // the feature disappears precisely when the arrangement is tidiest.
        yield return Unsorted(unsorted);
    }

    private ExplorerNodeViewModel Folder(
        CategoryFolder folder, IReadOnlyDictionary<string, ExplorerItem> items, int depth)
    {
        var node = new ExplorerNodeViewModel(ExplorerNodeKind.Group, _type, folder.Name, depth)
        {
            Folder = folder,
            IsLoaded = true
        };

        foreach (var child in Ordered(folder.Children))
            node.Children.Add(Folder(child, items, depth + 1));

        var members = folder.Members
            .Where(items.ContainsKey)
            .Select(resRef => items[resRef])
            .OrderBy(SortKey, StringComparer.CurrentCultureIgnoreCase);

        foreach (var item in members)
            node.Children.Add(Resource(item, LabelFor(item, insideFolder: true), depth + 1));

        node.Count = node.Children.Sum(child => child.IsResource ? 1 : child.Count);
        return node;
    }

    private ExplorerNodeViewModel Unsorted(IReadOnlyList<ExplorerItem> items)
    {
        var node = new ExplorerNodeViewModel(ExplorerNodeKind.Group, _type, _unsortedLabel, 0)
        {
            IsUnsorted = true,
            IsLoaded = true,
            Count = items.Count
        };

        foreach (var item in items.OrderBy(SortKey, StringComparer.CurrentCultureIgnoreCase))
            node.Children.Add(Resource(item, LabelFor(item, insideFolder: false), 1));

        return node;
    }

    /// <summary>
    /// What a row reads as. Inside a folder the host may drop the part its folders already say; outside
    /// one, and whenever the host has nothing shorter, it is the item's primary text.
    /// </summary>
    private string LabelFor(ExplorerItem item, bool insideFolder)
    {
        if (!insideFolder || _organization == null)
            return item.PrimaryText;

        var label = _organization.LeafLabel(_type, item);
        return label.Length > 0 ? label : item.PrimaryText;
    }

    private ExplorerNodeViewModel Resource(ExplorerItem item, string label, int depth) =>
        new(ExplorerNodeKind.Resource, _type, label, depth) { Item = item };

    /// <summary>Pinned folders first, then alphabetical - the order the palette's tree uses.</summary>
    private IEnumerable<CategoryFolder> Ordered(IReadOnlyList<CategoryFolder> folders)
    {
        var pinned = _section.Pinned;
        return folders
            .OrderBy(folder => pinned.Contains(folder.Name, StringComparer.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(folder => folder.Name, StringComparer.CurrentCultureIgnoreCase);
    }

    private static string SortKey(ExplorerItem item) => item.PrimaryText;
}
