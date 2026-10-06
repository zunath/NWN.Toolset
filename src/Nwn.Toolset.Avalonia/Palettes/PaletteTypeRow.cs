using CommunityToolkit.Mvvm.ComponentModel;

namespace Nwn.Toolset.Avalonia.Palettes;

/// <summary>Observable selection state for one shared palette type chip.</summary>
public partial class PaletteTypeRow : ObservableObject
{
    public PaletteTypeRow(PaletteTypeOption option, bool isSelected)
    {
        Option = option;
        IsSelected = isSelected;
    }

    public PaletteTypeOption Option { get; }

    public string Label => Option.Label;

    public string Initial => Option.Initial;

    public bool IsTiles => Option.IsTiles;

    public bool HasIcon => Option.HasIcon;

    public global::Avalonia.Media.Imaging.Bitmap? Icon => Option.Icon;

    [ObservableProperty]
    private bool _isSelected;
}
