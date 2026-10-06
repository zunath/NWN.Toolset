using Nwn.Authoring.Areas.Generation.Composition;

namespace Nwn.Toolset.Avalonia.Areas.Generation;

/// <summary>One tileset profile in the generator's picker; only its visual name is shown.</summary>
public sealed record AreaGeneratorTilesetChoice(DungeonTilesetProfile Value)
{
    public string Label => Value.DisplayName;
}
