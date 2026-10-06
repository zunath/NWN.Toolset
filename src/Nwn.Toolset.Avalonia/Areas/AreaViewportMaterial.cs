using Nwn.Preview.Pixels;

namespace Nwn.Toolset.Avalonia.Areas;

/// <summary>Decoded image surfaces and neutral draw parameters for one mesh material.</summary>
public sealed record AreaViewportMaterial
{
    public RgbaImage? Diffuse { get; init; }
    public RgbaImage? Normal { get; init; }
    public RgbaImage? Specular { get; init; }
    public RgbaImage? Roughness { get; init; }
    public RgbaImage? Environment { get; init; }
    public AreaViewportTintSurface? Tint { get; init; }
    public float AlphaCutoff { get; init; }
    public bool UseTextureAlpha { get; init; }
    public AreaViewportBlendMode BlendMode { get; init; }
}
