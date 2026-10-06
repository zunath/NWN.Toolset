using Nwn.Authoring.Resources;

namespace Nwn.Toolset.Avalonia.Palettes;

/// <summary>One immutable content view of a host's active blueprint or tile palette.</summary>
public sealed record PaletteSnapshot(
    long Revision,
    PaletteMode Mode,
    ModuleResourceType? SelectedType,
    PaletteSource Source,
    IReadOnlyList<PaletteTypeOption> Types,
    IReadOnlyList<PaletteCategorySnapshot> Categories,
    IReadOnlyList<PaletteEntrySnapshot> Entries,
    PaletteTilePaintMode TilePaintMode,
    bool HasOpenArea,
    string? StatusMessage,
    PaletteCapabilities Capabilities,
    double InitialTileSize = 136,
    double CategoryProportion = 0,
    PaletteCategoryId? InitialSelectedCategory = null);
