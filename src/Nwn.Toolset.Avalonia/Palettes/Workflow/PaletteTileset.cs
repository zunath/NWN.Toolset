using Nwn.Authoring.Areas.Tiles;

namespace Nwn.Toolset.Avalonia.Palettes.Workflow;

/// <summary>A loaded tileset: its palette and the name status messages call it by.</summary>
public sealed record PaletteTileset(TilePalette Palette, string DisplayName);
