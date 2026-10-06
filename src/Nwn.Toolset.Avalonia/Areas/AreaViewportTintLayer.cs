using System.Numerics;

namespace Nwn.Toolset.Avalonia.Areas;

/// <summary>Resolved color for one numeric layer in a host tint surface.</summary>
public readonly record struct AreaViewportTintLayer(Vector4 CustomColor, float PaletteRow);
