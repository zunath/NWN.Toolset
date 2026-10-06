using Nwn.Preview.Pixels;
using Nwn.Preview.Dds;

namespace Nwn.Preview.Plt;

/// <summary>Composites PLT shade/layer texels through caller-selected rows of a 256-column palette.</summary>
public static class PltCompositor
{
    public static RgbaImage Compose(PltTexture texture, RgbaImage palette,
        IReadOnlyDictionary<byte, int> rowByLayer, RasterDecodeOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(rowByLayer);
        return Compose(texture, palette, rowByLayer.ToDictionary(pair => pair.Key, pair => new PltLayerTint(pair.Value)), options);
    }

    /// <summary>Composites layers that use distinct host-selected palette images.</summary>
    public static RgbaImage Compose(PltTexture texture,
        IReadOnlyDictionary<byte, RgbaImage> paletteByLayer,
        IReadOnlyDictionary<byte, PltLayerTint> tintByLayer,
        RasterDecodeOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(texture);
        ArgumentNullException.ThrowIfNull(paletteByLayer);
        ArgumentNullException.ThrowIfNull(tintByLayer);
        options ??= new RasterDecodeOptions();
        options.Validate();
        var pixelCount = checked((long)texture.Width * texture.Height);
        if (texture.Width > options.MaximumWidth || texture.Height > options.MaximumHeight ||
            pixelCount > options.MaximumPixels || pixelCount * 4 > Array.MaxLength)
            throw new FormatException($"PLT composite surface {texture.Width}x{texture.Height} exceeds the configured output bounds.");

        var palettes = paletteByLayer.ToDictionary(pair => pair.Key, pair => pair.Value);
        var tints = tintByLayer.ToDictionary(pair => pair.Key, pair => pair.Value);
        foreach (var (layer, tint) in tints)
        {
            if (!palettes.TryGetValue(layer, out var palette))
                throw new FormatException($"No palette image was supplied for PLT layer id {layer}.");
            if (palette.Width != 256)
                throw new ArgumentException("A PLT palette image must have exactly 256 columns.", nameof(paletteByLayer));
            if (tint.PaletteRow < 0 || tint.PaletteRow >= palette.Height)
                throw new ArgumentOutOfRangeException(nameof(tintByLayer), "Every palette row must be within its palette image.");
        }

        var output = new byte[checked(texture.Width * texture.Height * 4)];
        for (var y = 0; y < texture.Height; y++)
        for (var x = 0; x < texture.Width; x++)
        {
            var texel = texture.GetPixel(x, y);
            if (!tints.TryGetValue(texel.LayerId, out var tint) || !palettes.TryGetValue(texel.LayerId, out var palette))
                throw new FormatException($"No palette settings were supplied for PLT layer id {texel.LayerId}.");
            var color = palette.GetPixel(texel.Shade, tint.PaletteRow);
            if (tint.ExactColor is { } exact)
            {
                var middle = palette.GetPixel(128, tint.PaletteRow);
                var factor = (color.R + color.G + color.B) / (double)Math.Max(3, middle.R + middle.G + middle.B);
                color = new RgbaPixel(Shade(exact.R, factor), Shade(exact.G, factor), Shade(exact.B, factor), exact.A);
            }
            var offset = checked((y * texture.Width + x) * 4);
            output[offset] = color.R;
            output[offset + 1] = color.G;
            output[offset + 2] = color.B;
            output[offset + 3] = color.A;
        }
        return new RgbaImage(texture.Width, texture.Height, output);
    }

    /// <summary>Resolves each texel before filtering, with caller-selected palette or exact-color policy per layer.</summary>
    public static RgbaImage Compose(PltTexture texture, RgbaImage palette,
        IReadOnlyDictionary<byte, PltLayerTint> tintByLayer, RasterDecodeOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(texture);
        ArgumentNullException.ThrowIfNull(palette);
        ArgumentNullException.ThrowIfNull(tintByLayer);
        options ??= new RasterDecodeOptions();
        options.Validate();
        var pixelCount = checked((long)texture.Width * texture.Height);
        if (texture.Width > options.MaximumWidth || texture.Height > options.MaximumHeight ||
            pixelCount > options.MaximumPixels || pixelCount * 4 > Array.MaxLength)
            throw new FormatException($"PLT composite surface {texture.Width}x{texture.Height} exceeds the configured output bounds.");
        if (palette.Width != 256)
            throw new ArgumentException("A PLT palette image must have exactly 256 columns.", nameof(palette));
        var tints = tintByLayer.ToDictionary(pair => pair.Key, pair => pair.Value);
        if (tints.Values.Any(tint => tint.PaletteRow < 0 || tint.PaletteRow >= palette.Height))
            throw new ArgumentOutOfRangeException(nameof(tintByLayer), "Every palette row must be within the palette image.");

        var output = new byte[checked(texture.Width * texture.Height * 4)];
        for (var y = 0; y < texture.Height; y++)
        for (var x = 0; x < texture.Width; x++)
        {
            var texel = texture.GetPixel(x, y);
            if (!tints.TryGetValue(texel.LayerId, out var tint))
                throw new FormatException($"No palette row was supplied for PLT layer id {texel.LayerId}.");
            var color = palette.GetPixel(texel.Shade, tint.PaletteRow);
            if (tint.ExactColor is { } exact)
            {
                var middle = palette.GetPixel(128, tint.PaletteRow);
                var factor = (color.R + color.G + color.B) / (double)Math.Max(3, middle.R + middle.G + middle.B);
                color = new RgbaPixel(Shade(exact.R, factor), Shade(exact.G, factor), Shade(exact.B, factor), exact.A);
            }
            var offset = checked((y * texture.Width + x) * 4);
            output[offset] = color.R;
            output[offset + 1] = color.G;
            output[offset + 2] = color.B;
            output[offset + 3] = color.A;
        }
        return new RgbaImage(texture.Width, texture.Height, output);
    }

    private static byte Shade(byte channel, double factor) =>
        (byte)Math.Clamp(Math.Round(channel * factor, MidpointRounding.AwayFromZero), 0, 255);
}
