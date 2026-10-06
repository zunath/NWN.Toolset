using Nwn.Authoring.Areas.Generation.Composition;

namespace Nwn.Toolset.Avalonia.Areas.Generation;

/// <summary>One tier of the selected theme, with its host-localized short label.</summary>
public sealed record AreaGeneratorTierChoice(DungeonTierDetail Value, string Label);
