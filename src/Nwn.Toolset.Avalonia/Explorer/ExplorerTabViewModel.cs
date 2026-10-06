using CommunityToolkit.Mvvm.ComponentModel;
using Nwn.Authoring.Resources;

namespace Nwn.Toolset.Avalonia.Explorer;

/// <summary>One tab of the Module Contents panel.</summary>
/// <remarks>
/// The count lives on the tab rather than on a row, so the sections a builder is not looking at still say
/// how much is in them - which is the only thing worth knowing about a section you are not in. It counts
/// everything of that type in the module, not what a search is currently showing.
/// </remarks>
public partial class ExplorerTabViewModel : ObservableObject
{
    public ExplorerTabViewModel(ModuleResourceType type, string label)
    {
        Type = type;
        Label = label;
    }

    public ModuleResourceType Type { get; }

    public string Label { get; }

    [ObservableProperty]
    private int _count;

    [ObservableProperty]
    private bool _isSelected;
}
