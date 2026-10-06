using Avalonia.Media.Imaging;
using Nwn.Authoring.Areas.Tiles;
using Nwn.Authoring.Resources;

namespace Nwn.Toolset.Avalonia.Palettes.Workflow;

/// <summary>The host's thumbnails: type-row icons and rendered previews for blueprints and tiles.</summary>
public interface IPalettePreviewSource
{
    /// <summary>Raised when a blueprint's cached preview is stale, so a visible tile re-requests it.</summary>
    event Action<ModuleResourceType, string>? Invalidated;

    /// <summary>False when no previews can be rendered; tiles then keep their glyphs.</summary>
    bool IsAvailable { get; }

    /// <summary>The type-row icon for a type, or for Tiles when <paramref name="type"/> is null.</summary>
    Bitmap? TypeIcon(ModuleResourceType? type);

    /// <summary>
    /// A blueprint's preview, or null when none can be rendered. <paramref name="cancellationToken"/> is
    /// cancelled when the tile is no longer wanted (scrolled away, a new snapshot); a host should stop
    /// rendering then.
    /// </summary>
    Task<Bitmap?> LoadBlueprintAsync(
        ModuleResourceType type,
        string resRef,
        PaletteSource source,
        CancellationToken cancellationToken);

    /// <summary>
    /// A tile or tile group's preview, or null when none can be rendered. <paramref name="cancellationToken"/>
    /// is cancelled when the request is no longer wanted.
    /// </summary>
    Task<Bitmap?> LoadTileAsync(TilePaletteEntry tile, CancellationToken cancellationToken);
}
