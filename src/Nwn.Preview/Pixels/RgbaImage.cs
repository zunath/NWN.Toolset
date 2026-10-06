using Nwn.Preview.Dds;

namespace Nwn.Preview.Pixels;

/// <summary>An immutable, top-down RGBA8 raster image.</summary>
public class RgbaImage
{
    private readonly byte[] _rgbaBytes;

    public int Width { get; }
    public int Height { get; }

    public RgbaImage(int width, int height, ReadOnlySpan<byte> rgbaBytes)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height));
        var expectedBytes = checked((long)width * height * 4);
        if (expectedBytes > Array.MaxLength || rgbaBytes.Length != expectedBytes)
            throw new ArgumentException("RGBA byte count must exactly match the image dimensions.", nameof(rgbaBytes));
        Width = width;
        Height = height;
        _rgbaBytes = rgbaBytes.ToArray();
    }

    public RgbaPixel GetPixel(int x, int y)
    {
        if ((uint)x >= (uint)Width)
            throw new ArgumentOutOfRangeException(nameof(x));
        if ((uint)y >= (uint)Height)
            throw new ArgumentOutOfRangeException(nameof(y));
        var offset = checked((y * Width + x) * 4);
        return new RgbaPixel(_rgbaBytes[offset], _rgbaBytes[offset + 1], _rgbaBytes[offset + 2], _rgbaBytes[offset + 3]);
    }

    public byte[] CopyRgbaBytes() => _rgbaBytes.ToArray();
}
