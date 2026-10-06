using Nwn.Authoring.Areas.Tiles;
using Nwn.Toolset.Avalonia.Palettes;
using Nwn.Toolset.Avalonia.Palettes.Workflow;

namespace Nwn.Toolset.Avalonia.Tests.Support;

internal sealed class FakePaletteSettings : IPaletteSettings
{
    public double PreviewSize { get; set; }

    public double CategoryProportion { get; set; }

    public PaletteSelection? Selection { get; set; }

    public PaletteSource Source { get; set; }

    public TilePaintMode? TilePaintMode { get; set; }
}
