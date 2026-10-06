using Nwn.Authoring.Areas.Generation.Composition;

namespace Nwn.Toolset.Avalonia.Areas.Generation;

/// <summary>One layout profile in the generator's picker.</summary>
public sealed record AreaGeneratorLayoutChoice(DungeonLayoutProfile Value)
{
    public string Label => Value.DisplayName;
}
