namespace Nwn.Toolset.Avalonia.Palettes;

/// <summary>Host policy for operations offered on one entry.</summary>
public sealed record PaletteEntryCapabilities(
    bool CanPlace,
    bool CanEdit,
    bool CanEditCopy,
    bool CanDelete,
    string? ReadOnlyNotice)
{
    public bool HasActions => CanEdit || CanEditCopy || CanDelete || !string.IsNullOrWhiteSpace(ReadOnlyNotice);
}
