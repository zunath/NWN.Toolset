using Nwn.Preview.Dds;

namespace Nwn.Preview.Plt;

/// <summary>A palette row, optionally used as the neutral shade ramp for an exact RGBA color.</summary>
public readonly record struct PltLayerTint(int PaletteRow, RgbaPixel? ExactColor = null);
