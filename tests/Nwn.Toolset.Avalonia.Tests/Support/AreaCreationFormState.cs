using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Nwn.Toolset.Avalonia.Areas;

namespace Nwn.Toolset.Avalonia.Tests.Support;

public sealed class AreaCreationFormState : IAreaCreationFormState
{
    public event PropertyChangedEventHandler? PropertyChanged { add { } remove { } }
    public string ResRef { get; set; } = "area_one";
    public string DisplayName { get; set; } = "First Area";
    public ObservableCollection<AreaTilesetChoice> Tilesets { get; } =
    [new("tms01", "Temperate"), new("tdt01", "Desert")];
    IEnumerable<AreaTilesetChoice> IAreaCreationFormState.Tilesets => Tilesets;
    public AreaTilesetChoice? SelectedTileset { get; set; }
    public double Width { get; set; } = 4;
    public double Height { get; set; } = 3;
    public string StatusMessage => string.Empty;
    public AreaCreationFormLabels Labels { get; } = new("New Area", "ResRef", "Display name", "Width", "Height", "Create", "Cancel");
    public int CreateCount { get; private set; }
    public int CancelCount { get; private set; }
    public ICommand CreateCommand { get; }
    public ICommand CancelCommand { get; }

    public AreaCreationFormState()
    {
        SelectedTileset = Tilesets[0];
        CreateCommand = new ActionCommand(() => CreateCount++);
        CancelCommand = new ActionCommand(() => CancelCount++);
    }

    private sealed class ActionCommand(Action action) : ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => action();
    }
}
