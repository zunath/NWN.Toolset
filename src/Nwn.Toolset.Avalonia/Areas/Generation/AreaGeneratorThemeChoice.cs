using Nwn.Authoring.Areas.Generation.Composition;

namespace Nwn.Toolset.Avalonia.Areas.Generation;

/// <summary>One theme in the generator's Theme picker.</summary>
public sealed record AreaGeneratorThemeChoice(DungeonDetail Value)
{
    public string Label => Value.DisplayName;
}
