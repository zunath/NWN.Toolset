using Nwn.Preview.Pixels;

namespace Nwn.Preview.Dds;

/// <summary>Immutable decoded top-level RGBA surface and its source format metadata.</summary>
public sealed class DdsImage : RgbaImage
{
    public int MipCount { get; }
    public DdsPixelFormat Format { get; }

    internal DdsImage(int width, int height, int mipCount, DdsPixelFormat format, byte[] rgbaBytes)
        : base(width, height, rgbaBytes)
    {
        MipCount = mipCount;
        Format = format;
    }
}
