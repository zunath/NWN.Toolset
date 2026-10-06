namespace Nwn.Authoring.Areas.Generation.Hosting;

/// <summary>The host's tilesets: which are available and how to load one for the solver.</summary>
public interface IAreaGenerationTilesetSource
{
    /// <summary>The ResRefs of every tileset the host can load.</summary>
    IReadOnlyCollection<string> GetTilesetResRefs();

    /// <summary>Loads one tileset, or returns false when it is unavailable.</summary>
    bool TryGetTileset(string tilesetResRef, out GeneratorTileset tileset);
}
