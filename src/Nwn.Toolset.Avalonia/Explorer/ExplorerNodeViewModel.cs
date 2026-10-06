using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Nwn.Authoring.Categories;
using Nwn.Authoring.Resources;

namespace Nwn.Toolset.Avalonia.Explorer;

/// <summary>One node of the Module Contents tree: a folder, the Unsorted bucket, or a resource.</summary>
/// <remarks>
/// Rows are published as one flat, virtualized list rather than a real TreeView: only nodes whose
/// ancestors are all expanded are realised, so a section with hundreds of resources in folders nobody
/// opened costs nothing to show.
/// </remarks>
public partial class ExplorerNodeViewModel : ObservableObject
{
    public ExplorerNodeViewModel(ExplorerNodeKind kind, ModuleResourceType type, string name, int depth)
    {
        Kind = kind;
        Type = type;
        Name = name;
        Depth = depth;
    }

    public ExplorerNodeKind Kind { get; }

    public ModuleResourceType Type { get; }

    public string Name { get; }

    public int Depth { get; }

    /// <summary>Set for resource nodes only.</summary>
    public ExplorerItem? Item { get; init; }

    /// <summary>
    /// The sidecar folder this row shows. Null on a resource row, and null on the Unsorted row - Unsorted
    /// is generated from what is filed nowhere, so there is nothing there to rename or delete, which is why
    /// the folder commands test this rather than the kind.
    /// </summary>
    public CategoryFolder? Folder { get; init; }

    /// <summary>True on the synthetic Unsorted row, the drop target that takes a resource out of every folder.</summary>
    public bool IsUnsorted { get; init; }

    public string ResRef => Item?.ResRef ?? string.Empty;

    public bool IsResource => Kind == ExplorerNodeKind.Resource;

    public bool IsBranch => Kind != ExplorerNodeKind.Resource;

    /// <summary>True for a real, editable folder row.</summary>
    public bool IsFolder => Folder != null;

    public ObservableCollection<ExplorerNodeViewModel> Children { get; } = new();

    /// <summary>True once this node's children have been built.</summary>
    public bool IsLoaded { get; set; }

    [ObservableProperty]
    private int _count;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Twisty))]
    private bool _isExpanded;

    /// <summary>Blank for resources, so only branches show a twisty.</summary>
    public string Twisty => IsResource ? string.Empty : IsExpanded ? "▾" : "▸";

    /// <summary>16px per level, with resources sitting one notch past their group.</summary>
    public Thickness Indent => new(6 + Depth * 16, 0, 0, 0);

    /// <summary>Types read as headings; groups as sub-headings; resources as content.</summary>
    public FontWeight Weight => Kind == ExplorerNodeKind.Type ? FontWeight.SemiBold : FontWeight.Normal;
}
