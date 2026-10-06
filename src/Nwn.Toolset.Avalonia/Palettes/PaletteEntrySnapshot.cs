using Nwn.Authoring.Resources;

namespace Nwn.Toolset.Avalonia.Palettes;

/// <summary>Immutable entry identity and display metadata supplied by a host.</summary>
public sealed record PaletteEntrySnapshot(
    PaletteEntryId Id,
    PaletteEntryKind Kind,
    ModuleResourceType? ResourceType,
    PaletteSource Source,
    string ResRef,
    string Name,
    string Subtitle,
    IReadOnlyList<PaletteCategoryId> CategoryIds,
    int? TileColumns,
    int? TileRows,
    IReadOnlyList<string> PreviewModelResRefs,
    PaletteEntryCapabilities Capabilities,
    bool SupportsPreview = true);
