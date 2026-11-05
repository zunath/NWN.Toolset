using System.Numerics;

namespace Nwn.Toolset.Avalonia.Areas;

/// <summary>Resolved color for one numeric layer in a host tint surface.</summary>
/// <param name="CustomColor">
/// Exact custom color in RGB with W above 0.5, or W at or below 0.5 to render the palette row.
/// </param>
/// <param name="PaletteRow">
/// Normalized palette texture row. For a custom color it is the host-selected nearest palette
/// swatch, whose row shades the color.
/// </param>
public readonly record struct AreaViewportTintLayer(Vector4 CustomColor, float PaletteRow);
