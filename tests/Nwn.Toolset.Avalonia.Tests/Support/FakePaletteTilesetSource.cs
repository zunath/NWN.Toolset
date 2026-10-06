using Nwn.Toolset.Avalonia.Palettes.Workflow;

namespace Nwn.Toolset.Avalonia.Tests.Support;

internal sealed class FakePaletteTilesetSource : IPaletteTilesetSource
{
    public Dictionary<string, PaletteTileset> Tilesets { get; } = new(StringComparer.OrdinalIgnoreCase);

    public PaletteTileset? Load(string tilesetResRef) =>
        Tilesets.TryGetValue(tilesetResRef, out var tileset) ? tileset : null;
}
