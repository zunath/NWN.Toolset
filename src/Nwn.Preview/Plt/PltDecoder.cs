using System.Buffers.Binary;
using Nwn.Preview.Pixels;

namespace Nwn.Preview.Plt;

/// <summary>Decodes the fixed-header, two-byte-per-texel PLT surface format.</summary>
public static class PltDecoder
{
    private const int HeaderSize = 24;

    public static PltTexture Decode(ReadOnlySpan<byte> bytes, RasterDecodeOptions? options = null)
    {
        options ??= new RasterDecodeOptions();
        options.Validate();
        if (bytes.Length < HeaderSize)
            throw new FormatException("PLT header is truncated; expected at least 24 bytes.");
        if (!bytes[..4].SequenceEqual("PLT "u8))
            throw new FormatException("PLT signature is invalid; expected 'PLT '.");
        if (!bytes.Slice(4, 4).SequenceEqual("V1  "u8))
            throw new NotSupportedException("PLT version is unsupported; expected 'V1  '.");

        var rawWidth = BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(16, 4));
        var rawHeight = BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(20, 4));
        if (rawWidth == 0 || rawHeight == 0 || rawWidth > options.MaximumWidth || rawHeight > options.MaximumHeight ||
            rawWidth > int.MaxValue || rawHeight > int.MaxValue)
            throw new FormatException($"PLT dimensions {rawWidth}x{rawHeight} are outside the configured bounds.");
        var width = (int)rawWidth;
        var height = (int)rawHeight;
        var pixelCount = checked((long)width * height);
        if (pixelCount > options.MaximumPixels || pixelCount * 2 > Array.MaxLength)
            throw new FormatException($"PLT surface {width}x{height} exceeds the configured allocation limit.");
        var payloadSize = checked((int)(pixelCount * 2));
        if (bytes.Length - HeaderSize < payloadSize)
            throw new FormatException($"PLT pixel payload is truncated: needs {payloadSize} bytes but has {bytes.Length - HeaderSize}.");

        var pixels = new byte[payloadSize];
        var source = bytes.Slice(HeaderSize, payloadSize);
        for (var y = 0; y < height; y++)
            source.Slice((height - 1 - y) * width * 2, width * 2).CopyTo(pixels.AsSpan(y * width * 2));
        return new PltTexture(width, height, pixels);
    }
}
