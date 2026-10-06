using Nwn.Preview.Pixels;

namespace Nwn.Toolset.Avalonia.Areas;

/// <summary>Neutral tint textures and their already-resolved layer colors.</summary>
public sealed class AreaViewportTintSurface
{
    private readonly IReadOnlyList<AreaViewportTintLayer> _layers;

    public RgbaImage Map { get; }
    public RgbaImage Palette { get; }
    public RgbaImage? Alpha { get; }
    public bool AlphaUsesRedChannel { get; }
    public float AlphaCutoff { get; }
    public IReadOnlyList<AreaViewportTintLayer> Layers => _layers;

    public AreaViewportTintSurface(
        RgbaImage map,
        RgbaImage palette,
        RgbaImage? alpha,
        bool alphaUsesRedChannel,
        float alphaCutoff,
        IEnumerable<AreaViewportTintLayer> layers)
    {
        Map = map ?? throw new ArgumentNullException(nameof(map));
        Palette = palette ?? throw new ArgumentNullException(nameof(palette));
        Alpha = alpha;
        AlphaUsesRedChannel = alphaUsesRedChannel;
        AlphaCutoff = alphaCutoff;
        var copiedLayers = layers?.ToArray() ?? throw new ArgumentNullException(nameof(layers));
        _layers = Array.AsReadOnly(copiedLayers);
        if (copiedLayers.Length != 10)
            throw new ArgumentException("A tint surface requires exactly ten numeric layer selections.", nameof(layers));
        if (!float.IsFinite(alphaCutoff) || alphaCutoff is < 0f or > 1f)
            throw new ArgumentOutOfRangeException(nameof(alphaCutoff));
        if (copiedLayers.Any(layer =>
                !float.IsFinite(layer.PaletteRow) || layer.PaletteRow is < 0f or > 1f ||
                !IsNormalized(layer.CustomColor.X) || !IsNormalized(layer.CustomColor.Y) ||
                !IsNormalized(layer.CustomColor.Z) || !IsNormalized(layer.CustomColor.W)))
        {
            throw new ArgumentException("Tint colors and palette coordinates must be finite normalized values.", nameof(layers));
        }
    }

    private static bool IsNormalized(float value) => float.IsFinite(value) && value is >= 0f and <= 1f;
}
