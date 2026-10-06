using Nwn.Authoring.Areas.Tiles;
using Nwn.Authoring.Resources;

namespace Nwn.Toolset.Avalonia.Palettes.Workflow;

/// <summary>What the palette needs from the area it places into.</summary>
public interface IPalettePlacementTarget
{
    /// <summary>
    /// The tileset this area is built from, or null when it cannot be resolved. Tiles mode lists this
    /// tileset's tiles.
    /// </summary>
    string? TilesetResRef { get; }

    /// <summary>
    /// Arms placement for a blueprint; the next click in the map resolves it. False when this area has
    /// no list for that type.
    /// </summary>
    bool ArmPlacement(ModuleResourceType type, string resRef, PaletteSource source);

    /// <summary>Arms placement for a tile or tile group. False when the area's grid cannot be edited.</summary>
    bool ArmTilePlacement(TilePaletteEntry entry);
}
