using Nwn.Preview.Pixels;
using Nwn.Preview.Plt;

namespace Nwn.Preview.Tint;

/// <summary>Decodes red-channel shades and equally binned green-channel layer identifiers.</summary>
public static class PackedTintMaskDecoder
{
    public static PltTexture Decode(RgbaImage mask, int layerCount, RasterDecodeOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(mask);
        if (layerCount is < 1 or > 256)
            throw new ArgumentOutOfRangeException(nameof(layerCount));
        options ??= new RasterDecodeOptions();
        options.Validate();
        var count = checked((long)mask.Width * mask.Height);
        if (mask.Width > options.MaximumWidth || mask.Height > options.MaximumHeight ||
            count > options.MaximumPixels || count * 2 > Array.MaxLength)
            throw new FormatException("Packed tint mask exceeds its configured raster bounds.");
        var pixels = new byte[checked(mask.Width * mask.Height * 2)];
        for (var y = 0; y < mask.Height; y++)
        for (var x = 0; x < mask.Width; x++)
        {
            var pixel = mask.GetPixel(x, y);
            var offset = checked((y * mask.Width + x) * 2);
            pixels[offset] = pixel.R;
            pixels[offset + 1] = (byte)Math.Min(layerCount - 1, pixel.G * layerCount / 255);
        }
        return new PltTexture(mask.Width, mask.Height, pixels);
    }
}
