#nullable disable
using Nwn.Authoring.Areas.Generation.Composition;
using Nwn.Authoring.Areas.Generation.Tilesets;

namespace Nwn.Authoring.Areas.Generation.Drafting
{
    /// <summary>A solved request plus the exact definitions used to produce it.</summary>
    /// <param name="Settings">The request that was solved.</param>
    /// <param name="Composition">The theme, tileset profile and layout profile composed for it.</param>
    /// <param name="Tileset">The tileset the layout was solved against.</param>
    /// <param name="Result">The solved layout and planned dressing.</param>
    /// <param name="TilesetFingerprint">The host's token for the tileset source version, or empty.</param>
    public sealed record AreaGenerationDraft(
        AreaGenerationSettings Settings,
        DungeonComposition Composition,
        TilesetModel Tileset,
        GenerationResult Result,
        string TilesetFingerprint = "");
}
