// SPDX-License-Identifier: MIT

using System.ComponentModel;
using System.Windows.Input;

namespace Nwn.Toolset.Avalonia.Areas;

/// <summary>Observable selection readout and host actions used by the shared scene context menu.</summary>
public interface IAreaSelectionContextMenuState : INotifyPropertyChanged
{
    string SelectionName { get; }
    string SelectionGlyph { get; }
    string SelectionKindLabel { get; }
    string SelectionResRef { get; }
    bool CanOpenProperties { get; }
    bool CanEditBlueprint { get; }
    bool CanEditCopy { get; }
    ICommand OpenPropertiesCommand { get; }
    ICommand EditBlueprintCommand { get; }
    ICommand EditCopyCommand { get; }
}
