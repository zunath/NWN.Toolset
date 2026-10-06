using Nwn.Preview.Dds;
using Nwn.Preview.Pixels;

namespace Nwn.Preview.Tga;

/// <summary>Decodes bounded 8-bit grayscale and 24/32-bit true-color TGA images.</summary>
public static class TgaDecoder
{
    private const int HeaderSize = 18;

    public static RgbaImage Decode(ReadOnlySpan<byte> bytes, RasterDecodeOptions? options = null)
    {
        options ??= new RasterDecodeOptions();
        options.Validate();
        if (bytes.Length < HeaderSize)
            throw new FormatException("TGA header is truncated; expected at least 18 bytes.");

        var colorMapType = bytes[1];
        var imageType = bytes[2];
        if (colorMapType != 0)
            throw new NotSupportedException("Color-mapped TGA images are unsupported.");
        var grayscale = imageType is 3 or 11;
        var rle = imageType is 10 or 11;
        if (!grayscale && imageType is not (2 or 10))
            throw new NotSupportedException($"TGA image type {imageType} is unsupported; supported types are 2, 3, 10, and 11.");
        if (bytes[17] >> 6 != 0)
            throw new FormatException("TGA interleaving descriptor bits are unsupported.");

        var width = ReadUInt16(bytes, 12);
        var height = ReadUInt16(bytes, 14);
        ValidateDimensions(width, height, options);
        var depth = bytes[16];
        var bytesPerPixel = grayscale ? 1 : depth / 8;
        if ((grayscale && depth != 8) || (!grayscale && depth is not (24 or 32)))
            throw new NotSupportedException($"TGA pixel depth {depth} is unsupported for image type {imageType}.");
        var alphaBits = bytes[17] & 0x0F;
        // NWN's stock pmh0_bicepl007.tga is 8-bit grayscale with 8 descriptor attribute bits.
        // Its 512-byte pixel payload contains one luminance byte per pixel and decodes as opaque intensity.
        var nwnOpaqueGrayscaleCompatibility = grayscale && depth == 8 && alphaBits == 8;
        if ((!grayscale && depth == 32 && alphaBits is not (0 or 8)) ||
            (!grayscale && depth == 24 && alphaBits != 0) ||
            (grayscale && alphaBits != 0 && !nwnOpaqueGrayscaleCompatibility))
            throw new FormatException("TGA alpha attribute bits do not match the declared pixel depth.");

        var pixelCount = checked(width * height);
        var output = new byte[checked(pixelCount * 4)];
        var offset = HeaderSize + bytes[0];
        if (offset > bytes.Length)
            throw new FormatException("TGA image identifier extends beyond the input.");

        var cursor = 0;
        while (cursor < pixelCount)
        {
            var packetCount = 1;
            var repeated = false;
            if (rle)
            {
                if (offset >= bytes.Length)
                    throw new FormatException("TGA RLE packet header is truncated.");
                var packetHeader = bytes[offset++];
                packetCount = (packetHeader & 0x7F) + 1;
                repeated = (packetHeader & 0x80) != 0;
                if (packetCount > pixelCount - cursor)
                    throw new FormatException("TGA RLE packet exceeds the declared image pixel count.");
            }

            if (repeated)
            {
                var pixel = ReadPixel(bytes, ref offset, bytesPerPixel, grayscale, alphaBits == 8);
                for (var i = 0; i < packetCount; i++)
                    WriteFileOrderPixel(output, cursor++, pixel, width, height, bytes[17]);
            }
            else
            {
                for (var i = 0; i < packetCount; i++)
                {
                    var pixel = ReadPixel(bytes, ref offset, bytesPerPixel, grayscale, alphaBits == 8);
                    WriteFileOrderPixel(output, cursor++, pixel, width, height, bytes[17]);
                }
            }
        }

        return new RgbaImage(width, height, output);
    }

    private static RgbaPixel ReadPixel(ReadOnlySpan<byte> bytes, ref int offset, int bytesPerPixel,
        bool grayscale, bool hasAlpha)
    {
        if (bytes.Length - offset < bytesPerPixel)
            throw new FormatException("TGA pixel payload is truncated.");
        RgbaPixel pixel;
        if (grayscale)
        {
            var intensity = bytes[offset];
            pixel = new RgbaPixel(intensity, intensity, intensity, 255);
        }
        else
        {
            pixel = new RgbaPixel(bytes[offset + 2], bytes[offset + 1], bytes[offset],
                hasAlpha ? bytes[offset + 3] : (byte)255);
        }
        offset += bytesPerPixel;
        return pixel;
    }

    private static void WriteFileOrderPixel(byte[] output, int cursor, RgbaPixel pixel, int width, int height, byte descriptor)
    {
        var sourceX = cursor % width;
        var sourceY = cursor / width;
        var x = (descriptor & 0x10) != 0 ? width - 1 - sourceX : sourceX;
        var y = (descriptor & 0x20) == 0 ? height - 1 - sourceY : sourceY;
        var offset = checked((y * width + x) * 4);
        output[offset] = pixel.R;
        output[offset + 1] = pixel.G;
        output[offset + 2] = pixel.B;
        output[offset + 3] = pixel.A;
    }

    private static void ValidateDimensions(int width, int height, RasterDecodeOptions options)
    {
        var pixels = checked((long)width * height);
        if (width == 0 || height == 0 || width > options.MaximumWidth || height > options.MaximumHeight ||
            pixels > options.MaximumPixels || pixels * 4 > Array.MaxLength)
            throw new FormatException($"TGA dimensions {width}x{height} are outside the configured bounds.");
    }

    private static int ReadUInt16(ReadOnlySpan<byte> bytes, int offset) => bytes[offset] | (bytes[offset + 1] << 8);
}
