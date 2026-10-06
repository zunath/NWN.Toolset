using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;

namespace Nwn.Toolset.Avalonia.Areas;

/// <summary>Binding contract for host-owned area creation state and commands.</summary>
public interface IAreaCreationFormState : INotifyPropertyChanged
{
    string ResRef { get; set; }
    string DisplayName { get; set; }
    IEnumerable<AreaTilesetChoice> Tilesets { get; }
    AreaTilesetChoice? SelectedTileset { get; set; }
    double Width { get; set; }
    double Height { get; set; }
    string StatusMessage { get; }
    ICommand CreateCommand { get; }
    ICommand CancelCommand { get; }
    AreaCreationFormLabels Labels { get; }
}
