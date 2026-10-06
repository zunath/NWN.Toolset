namespace Nwn.Toolset.Avalonia.Palettes;

/// <summary>Host policy for operations offered on a category row.</summary>
public sealed record PaletteCategoryCapabilities(
    bool CanCreateBlueprint,
    bool CanCreateCategory,
    bool CanRename,
    bool CanDelete,
    bool CanPin,
    bool CanFileSelectedEntry,
    string? ReadOnlyNotice);
