namespace Nwn.Preview.Plt;

/// <summary>An immutable top-down PLT image retaining per-pixel shade and layer data.</summary>
public sealed class PltTexture
{
    private readonly byte[] _pixels;

    public int Width { get; }
    public int Height { get; }

    internal PltTexture(int width, int height, byte[] topDownPixels)
    {
        Width = width;
        Height = height;
        _pixels = topDownPixels.ToArray();
    }

    public PltPixel GetPixel(int x, int y)
    {
        if ((uint)x >= (uint)Width)
            throw new ArgumentOutOfRangeException(nameof(x));
        if ((uint)y >= (uint)Height)
            throw new ArgumentOutOfRangeException(nameof(y));
        var offset = checked((y * Width + x) * 2);
        return new PltPixel(_pixels[offset], _pixels[offset + 1]);
    }

    public byte[] CopyPixelBytes() => _pixels.ToArray();
}
