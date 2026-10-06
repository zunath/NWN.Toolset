using Nwn.Authoring.Resources;

namespace Nwn.Toolset.Avalonia.Palettes.Workflow;

/// <summary>A saved palette selection: Tiles, or one blueprint type.</summary>
public readonly record struct PaletteSelection(PaletteMode Mode, ModuleResourceType? Type)
{
    public static PaletteSelection Tiles { get; } = new(PaletteMode.Tiles, null);

    public static PaletteSelection ForType(ModuleResourceType type) => new(PaletteMode.Blueprints, type);
}
