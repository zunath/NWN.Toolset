namespace Nwn.Toolset.Avalonia.Palettes;

/// <summary>Immutable host-supplied category node. Entries can belong to multiple categories.</summary>
public sealed record PaletteCategorySnapshot(
    PaletteCategoryId Id,
    string Name,
    int Count,
    bool IsPinned,
    int PinOrder,
    IReadOnlyList<PaletteCategorySnapshot> Children,
    IReadOnlyList<PaletteEntryId> EntryIds,
    PaletteCategoryCapabilities Capabilities);
