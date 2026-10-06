using Avalonia.Media.Imaging;
using Nwn.Authoring.Resources;

namespace Nwn.Toolset.Avalonia.Palettes;

/// <summary>A selectable blueprint type or the tiles mode chip.</summary>
public sealed record PaletteTypeOption(
    ModuleResourceType? Type,
    string Label,
    string Initial,
    string NewBlueprintLabel,
    Bitmap? Icon)
{
    public bool IsTiles => Type is null;

    public bool HasIcon => Icon is not null;
}
