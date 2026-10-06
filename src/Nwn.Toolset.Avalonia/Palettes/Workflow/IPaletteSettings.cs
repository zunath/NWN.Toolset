using Nwn.Authoring.Areas.Tiles;

namespace Nwn.Toolset.Avalonia.Palettes.Workflow;

/// <summary>Where the palette keeps the builder's preferences between sessions.</summary>
public interface IPaletteSettings
{
    /// <summary>Preview tile width in pixels, or 0 or less for the panel's default.</summary>
    double PreviewSize { get; set; }

    /// <summary>Share of the panel's height the category tree keeps, or 0 when never moved.</summary>
    double CategoryProportion { get; set; }

    /// <summary>What the palette was last showing, or null when nothing was saved.</summary>
    PaletteSelection? Selection { get; set; }

    /// <summary>Which side of the palette was showing.</summary>
    PaletteSource Source { get; set; }

    /// <summary>The last tile paint mode, or null when nothing valid was saved.</summary>
    TilePaintMode? TilePaintMode { get; set; }
}
