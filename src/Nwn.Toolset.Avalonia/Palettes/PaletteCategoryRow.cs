using CommunityToolkit.Mvvm.ComponentModel;

namespace Nwn.Toolset.Avalonia.Palettes;

/// <summary>A flattened row in the virtualized palette category tree.</summary>
public partial class PaletteCategoryRow : ObservableObject
{
    public PaletteCategoryRow(
        PaletteCategorySnapshot category,
        int depth,
        bool hasChildren,
        bool isPinnedRow = false)
    {
        Category = category ?? throw new ArgumentNullException(nameof(category));
        Id = category.Id;
        Name = category.Name;
        Count = category.Count;
        Depth = depth;
        HasChildren = hasChildren;
        IsPinned = isPinnedRow;
        Capabilities = category.Capabilities;
    }

    public PaletteCategorySnapshot Category { get; }

    public PaletteCategoryId Id { get; }

    public string Name { get; }

    public int Count { get; }

    public int Depth { get; }

    public bool HasChildren { get; }

    public bool IsPinned { get; }

    public PaletteCategoryCapabilities Capabilities { get; }

    public global::Avalonia.Thickness Indent => new(11 + Depth * 15, 0, 0, 0);

    public string Twisty => !HasChildren ? string.Empty : IsExpanded ? "▾" : "▸";

    [ObservableProperty]
    private bool _isExpanded;
}
