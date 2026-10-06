namespace Nwn.Toolset.Avalonia.Palettes;

/// <summary>Host policy controlling palette-level source and type operations.</summary>
public sealed record PaletteCapabilities(
    bool CanSelectSource,
    bool CanSelectType,
    bool CanSelectMode,
    bool CanSelectTilePaintMode);
