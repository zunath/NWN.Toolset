using Nwn.Authoring.Areas.Generation.Composition;
using Nwn.Authoring.Areas.Generation.Decoration;

namespace Nwn.Authoring.Areas.Generation.Hosting;

/// <summary>
/// An in-memory <see cref="IAreaGenerationCatalog"/> built from a host's definitions. Themes list by display
/// name, and palette variants of tileset profiles inherit their family's dressing once, here, so every
/// later read of a profile already reflects its effective palette.
/// </summary>
public sealed class AreaGenerationCatalog : IAreaGenerationCatalog
{
    public IReadOnlyList<DungeonDetail> Themes { get; }
    public IReadOnlyDictionary<string, DungeonTilesetProfile> TilesetProfiles { get; }
    public IReadOnlyDictionary<string, DungeonLayoutProfile> LayoutProfiles { get; }

    /// <summary>Builds a catalog; a later definition with the same key replaces an earlier one.</summary>
    public AreaGenerationCatalog(
        IEnumerable<DungeonDetail> themes,
        IEnumerable<DungeonTilesetProfile> tilesetProfiles,
        IEnumerable<DungeonLayoutProfile> layoutProfiles)
    {
        ArgumentNullException.ThrowIfNull(themes);
        ArgumentNullException.ThrowIfNull(tilesetProfiles);
        ArgumentNullException.ThrowIfNull(layoutProfiles);

        var themeByKey = new Dictionary<string, DungeonDetail>();
        foreach (var theme in themes)
            themeByKey[theme.ThemeKey] = theme;
        Themes = themeByKey.Values.OrderBy(theme => theme.DisplayName).ToList();

        var tilesets = new Dictionary<string, DungeonTilesetProfile>();
        foreach (var profile in tilesetProfiles)
            tilesets[profile.Key] = profile;
        DungeonTilesetPaletteInheritance.Apply(tilesets);
        TilesetProfiles = tilesets;

        var layouts = new Dictionary<string, DungeonLayoutProfile>();
        foreach (var profile in layoutProfiles)
            layouts[profile.Key] = profile;
        LayoutProfiles = layouts;
    }
}
