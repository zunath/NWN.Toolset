namespace Nwn.Toolset.Avalonia.Palettes.Workflow;

/// <summary>The host's tilesets, shaped into tile palettes.</summary>
public interface IPaletteTilesetSource
{
    /// <summary>The tile palette and display name for a tileset, or null when it cannot be loaded.</summary>
    PaletteTileset? Load(string tilesetResRef);
}
