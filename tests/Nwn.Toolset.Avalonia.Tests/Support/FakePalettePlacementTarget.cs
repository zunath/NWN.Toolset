using Nwn.Authoring.Areas.Tiles;
using Nwn.Authoring.Resources;
using Nwn.Toolset.Avalonia.Palettes;
using Nwn.Toolset.Avalonia.Palettes.Workflow;

namespace Nwn.Toolset.Avalonia.Tests.Support;

internal sealed class FakePalettePlacementTarget : IPalettePlacementTarget
{
    public string? TilesetResRef { get; set; }

    public bool Accepts { get; set; } = true;

    public List<(ModuleResourceType Type, string ResRef, PaletteSource Source)> Armed { get; } = new();

    public bool ArmPlacement(ModuleResourceType type, string resRef, PaletteSource source)
    {
        Armed.Add((type, resRef, source));
        return Accepts;
    }

    public bool ArmTilePlacement(TilePaletteEntry entry) => Accepts;
}
