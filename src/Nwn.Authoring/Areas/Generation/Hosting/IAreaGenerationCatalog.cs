using Nwn.Authoring.Areas.Generation.Composition;

namespace Nwn.Authoring.Areas.Generation.Hosting;

/// <summary>
/// The content a host offers the generator: the themes, tileset profiles and layout profiles its
/// pickers list. Every game-specific name, resource reference and tuning value lives behind this
/// interface; the generator itself carries none. A host with no themes is valid and generates
/// geometry only.
/// </summary>
public interface IAreaGenerationCatalog
{
    /// <summary>Themes in the order their picker lists them. May be empty.</summary>
    IReadOnlyList<DungeonDetail> Themes { get; }

    /// <summary>Tileset profiles by key.</summary>
    IReadOnlyDictionary<string, DungeonTilesetProfile> TilesetProfiles { get; }

    /// <summary>Layout profiles by key.</summary>
    IReadOnlyDictionary<string, DungeonLayoutProfile> LayoutProfiles { get; }
}
