using System.Buffers.Binary;
using System.Numerics;
using System.Text;

namespace Nwn.Preview.Dds;

/// <summary>Decodes bounded DDS DXT1/3/5, unsigned ATI2 and masked RGB surfaces to top-down RGBA.</summary>
public static class DdsDecoder
{
    private const uint DdsMagic = 0x20534444;
    private const uint FourCcFlag = 0x4;
    private const uint RgbFlag = 0x40;
    private const uint PitchFlag = 0x8;
    private const int HeaderSize = 128;
    private const int Dxt1BlockBytes = 8;
    private const int DxtBlockBytes = 16;
    private const int CompactHeaderSize = 20;

    public static DdsImage Decode(ReadOnlySpan<byte> bytes, DdsDecodeOptions? options = null)
    {
        options ??= new DdsDecodeOptions();
        ValidateOptions(options);
        if (LooksLikeCompactBioware(bytes))
            return DecodeCompactBioware(bytes, options);
        if (bytes.Length < HeaderSize)
            throw new FormatException("Standard DDS header is truncated; expected at least 128 bytes.");
        if (ReadUInt32(bytes, 0) != DdsMagic)
        {
            throw new FormatException("DDS signature is invalid; expected the standard 'DDS ' magic.");
        }
        if (ReadUInt32(bytes, 4) != 124)
            throw new FormatException("DDS header size must be 124 bytes.");
        if (ReadUInt32(bytes, 76) != 32)
            throw new FormatException("DDS pixel-format header size must be 32 bytes.");

        var flags = ReadUInt32(bytes, 8);
        var rawHeight = ReadUInt32(bytes, 12);
        var rawWidth = ReadUInt32(bytes, 16);
        if (rawWidth == 0 || rawHeight == 0 || rawWidth > options.MaximumWidth || rawHeight > options.MaximumHeight)
            throw new FormatException($"DDS dimensions {rawWidth}x{rawHeight} are outside the configured bounds.");
        var width = (int)rawWidth;
        var height = (int)rawHeight;
        var pixelCount = checked((long)width * height);
        if (pixelCount > options.MaximumPixels || pixelCount * 4 > Array.MaxLength)
            throw new FormatException($"DDS surface {width}x{height} exceeds the configured pixel allocation limit.");

        var mipCountRaw = ReadUInt32(bytes, 28);
        var mipCount = mipCountRaw == 0 ? 1 : mipCountRaw > int.MaxValue ? int.MaxValue : (int)mipCountRaw;
        var maximumMips = 1 + BitOperations.Log2((uint)Math.Max(width, height));
        if (mipCount > options.MaximumMipLevels || mipCount > maximumMips)
            throw new FormatException($"DDS mip count {mipCountRaw} exceeds the valid chain for {width}x{height}.");

        var pixelFlags = ReadUInt32(bytes, 80);
        DdsPixelFormat format;
        if ((pixelFlags & FourCcFlag) != 0)
        {
            var fourCc = Encoding.ASCII.GetString(bytes.Slice(84, 4));
            format = fourCc switch
            {
                "DXT1" => DdsPixelFormat.Bc1,
                "DXT3" => DdsPixelFormat.Bc2,
                "DXT5" => DdsPixelFormat.Bc3,
                "ATI2" => DdsPixelFormat.Bc5,
                _ => throw new NotSupportedException($"DDS FourCC '{fourCc}' is unsupported; supported formats are DXT1, DXT3, DXT5, and unsigned ATI2.")
            };
        }
        else if ((pixelFlags & RgbFlag) != 0)
        {
            var bitCount = ReadUInt32(bytes, 88);
            format = bitCount switch
            {
                24 => DdsPixelFormat.Rgb24,
                32 => DdsPixelFormat.Rgba32,
                _ => throw new NotSupportedException($"Uncompressed DDS RGB bit depth {bitCount} is unsupported; expected 24 or 32 bits.")
            };
            ValidateMasks(bytes, bitCount);
        }
        else
        {
            throw new NotSupportedException("DDS pixel format is unsupported; expected DXT1/3/5, unsigned ATI2 or masked 24/32-bit RGB.");
        }

        var requiredBytes = RequiredPayloadBytes(bytes, flags, width, height, mipCount, format);
        if (requiredBytes > bytes.Length - HeaderSize)
            throw new FormatException($"DDS mip payload is truncated: needs {requiredBytes} bytes but has {bytes.Length - HeaderSize}.");

        var pixels = new byte[checked((int)(pixelCount * 4))];
        if (format is DdsPixelFormat.Bc1 or DdsPixelFormat.Bc2 or DdsPixelFormat.Bc3 or DdsPixelFormat.Bc5)
            DecodeBlocks(bytes[HeaderSize..], width, height, format, pixels);
        else
            DecodeRgb(bytes[HeaderSize..], bytes, flags, width, height, format, pixels);

        NormalizeRows(pixels, width, height, options.StoredRowOrder, DdsStoredRowOrder.TopDown);

        return new DdsImage(width, height, mipCount, format, pixels);
    }

    private static DdsImage DecodeCompactBioware(ReadOnlySpan<byte> bytes, DdsDecodeOptions options)
    {
        if (bytes.Length < CompactHeaderSize)
            throw new FormatException("Compact BioWare DDS header is truncated; expected 20 bytes.");
        var rawWidth = ReadUInt32(bytes, 0);
        var rawHeight = ReadUInt32(bytes, 4);
        var width = ValidateCompactDimension(rawWidth, options.MaximumWidth, "width");
        var height = ValidateCompactDimension(rawHeight, options.MaximumHeight, "height");
        var formatCode = bytes[8];
        var format = formatCode switch
        {
            3 => DdsPixelFormat.Bc1,
            4 => DdsPixelFormat.Bc3,
            _ => throw new NotSupportedException($"Compact BioWare DDS format code {formatCode} is unsupported; supported codes are 3 (DXT1) and 4 (DXT5).")
        };

        var pixelCount = checked((long)width * height);
        if (pixelCount > options.MaximumPixels || pixelCount * 4 > Array.MaxLength)
            throw new FormatException($"Compact BioWare DDS surface {width}x{height} exceeds the configured pixel allocation limit.");

        var blockBytes = format == DdsPixelFormat.Bc1 ? Dxt1BlockBytes : DxtBlockBytes;
        var expectedBaseSize = checked(((width + 3L) / 4) * ((height + 3L) / 4) * blockBytes);
        var declaredBaseSize = ReadUInt32(bytes, 12);
        if (declaredBaseSize != expectedBaseSize)
            throw new FormatException($"Compact BioWare DDS base level size {declaredBaseSize} does not match its {width}x{height} format size {expectedBaseSize}.");

        var mipCount = 1 + System.Numerics.BitOperations.Log2((uint)Math.Max(width, height));
        if (mipCount > options.MaximumMipLevels)
            throw new FormatException($"Compact BioWare DDS mip count {mipCount} exceeds the configured limit {options.MaximumMipLevels}.");
        long payloadSize = 0;
        var mipWidth = width;
        var mipHeight = height;
        for (var level = 0; level < mipCount; level++)
        {
            payloadSize = checked(payloadSize + ((mipWidth + 3L) / 4) * ((mipHeight + 3L) / 4) * blockBytes);
            mipWidth = Math.Max(1, mipWidth / 2);
            mipHeight = Math.Max(1, mipHeight / 2);
        }
        if (payloadSize > bytes.Length - CompactHeaderSize)
            throw new FormatException($"Compact BioWare DDS mip payload is truncated: needs {payloadSize} bytes but has {bytes.Length - CompactHeaderSize}.");

        var pixels = new byte[checked((int)(pixelCount * 4))];
        DecodeBlocks(bytes[CompactHeaderSize..], width, height, format, pixels);
        NormalizeRows(pixels, width, height, options.StoredRowOrder, DdsStoredRowOrder.BottomUp);
        return new DdsImage(width, height, mipCount, format, pixels);
    }

    private static int ValidateCompactDimension(uint value, int maximum, string name)
    {
        if (value == 0 || value > maximum || value > int.MaxValue)
            throw new FormatException($"Compact BioWare DDS {name} {value} is outside the configured bounds.");
        return (int)value;
    }

    private static long RequiredPayloadBytes(
        ReadOnlySpan<byte> bytes,
        uint flags,
        int width,
        int height,
        int mipCount,
        DdsPixelFormat format)
    {
        long total = 0;
        var mipWidth = width;
        var mipHeight = height;
        var compressed = format is DdsPixelFormat.Bc1 or DdsPixelFormat.Bc2 or DdsPixelFormat.Bc3 or DdsPixelFormat.Bc5;
        var blockBytes = format == DdsPixelFormat.Bc1 ? Dxt1BlockBytes : DxtBlockBytes;
        var topPitch = !compressed && (flags & PitchFlag) != 0 ? ReadUInt32(bytes, 20) : 0;
        var topRowBytes = checked((long)width * (format == DdsPixelFormat.Rgb24 ? 3 : 4));
        if (!compressed && topPitch != 0 && topPitch < topRowBytes)
            throw new FormatException("DDS row pitch is smaller than the declared RGB pixel row.");

        for (var level = 0; level < mipCount; level++)
        {
            long levelBytes;
            if (compressed)
            {
                var blocksWide = (mipWidth + 3L) / 4;
                var blocksHigh = (mipHeight + 3L) / 4;
                levelBytes = checked(blocksWide * blocksHigh * blockBytes);
            }
            else
            {
                var rowBytes = checked((long)mipWidth * (format == DdsPixelFormat.Rgb24 ? 3 : 4));
                var pitch = level == 0 && topPitch != 0 ? topPitch : rowBytes;
                levelBytes = checked(pitch * mipHeight);
            }
            total = checked(total + levelBytes);
            mipWidth = Math.Max(1, mipWidth / 2);
            mipHeight = Math.Max(1, mipHeight / 2);
        }
        return total;
    }

    private static void DecodeBlocks(ReadOnlySpan<byte> data, int width, int height, DdsPixelFormat format, byte[] output)
    {
        var blockBytes = format == DdsPixelFormat.Bc1 ? Dxt1BlockBytes : DxtBlockBytes;
        var blocksWide = (width + 3) / 4;
        var blocksHigh = (height + 3) / 4;
        var offset = 0;
        Span<RgbaPixel> colors = stackalloc RgbaPixel[4];
        Span<byte> alphaPalette = stackalloc byte[8];
        Span<byte> greenPalette = stackalloc byte[8];
        for (var blockY = 0; blockY < blocksHigh; blockY++)
        for (var blockX = 0; blockX < blocksWide; blockX++)
        {
            var block = data.Slice(offset, blockBytes);
            if (format == DdsPixelFormat.Bc5)
            {
                DecodeInterpolatedChannel(block[..8], alphaPalette, out var redIndices);
                DecodeInterpolatedChannel(block[8..], greenPalette, out var greenIndices);
                for (var pixel = 0; pixel < 16; pixel++)
                {
                    var x = blockX * 4 + pixel % 4;
                    var y = blockY * 4 + pixel / 4;
                    if (x < width && y < height)
                        WritePixel(output, width, x, y, new RgbaPixel(
                            alphaPalette[(int)((redIndices >> (pixel * 3)) & 7)],
                            greenPalette[(int)((greenIndices >> (pixel * 3)) & 7)], 0, 255));
                }
                offset += blockBytes;
                continue;
            }
            var colorOffset = format == DdsPixelFormat.Bc1 ? 0 : 8;
            DecodeColors(block[colorOffset..], colors, forceFourColor: format != DdsPixelFormat.Bc1);
            var colorIndices = BinaryPrimitives.ReadUInt32LittleEndian(block.Slice(colorOffset + 4, 4));
            ulong alphaBits = 0;
            if (format == DdsPixelFormat.Bc2)
                alphaBits = BinaryPrimitives.ReadUInt64LittleEndian(block[..8]);
            else if (format == DdsPixelFormat.Bc3)
                DecodeInterpolatedChannel(block[..8], alphaPalette, out alphaBits);

            for (var pixel = 0; pixel < 16; pixel++)
            {
                var x = blockX * 4 + pixel % 4;
                var y = blockY * 4 + pixel / 4;
                if (x >= width || y >= height)
                    continue;
                var color = colors[(int)((colorIndices >> (pixel * 2)) & 3)];
                if (format == DdsPixelFormat.Bc2)
                {
                    var nibble = (byte)((alphaBits >> (pixel * 4)) & 0xF);
                    color = color with { A = (byte)(nibble * 17) };
                }
                else if (format == DdsPixelFormat.Bc3)
                {
                    color = color with { A = alphaPalette[(int)((alphaBits >> (pixel * 3)) & 7)] };
                }
                WritePixel(output, width, x, y, color);
            }
            offset += blockBytes;
        }
    }

    private static void DecodeColors(ReadOnlySpan<byte> block, Span<RgbaPixel> colors, bool forceFourColor)
    {
        var color0 = BinaryPrimitives.ReadUInt16LittleEndian(block[..2]);
        var color1 = BinaryPrimitives.ReadUInt16LittleEndian(block.Slice(2, 2));
        colors[0] = Expand565(color0, 255);
        colors[1] = Expand565(color1, 255);
        if (forceFourColor || color0 > color1)
        {
            colors[2] = Interpolate(colors[0], colors[1], 2, 1, 3);
            colors[3] = Interpolate(colors[0], colors[1], 1, 2, 3);
        }
        else
        {
            colors[2] = Interpolate(colors[0], colors[1], 1, 1, 2);
            colors[3] = new RgbaPixel(0, 0, 0, 0);
        }
    }

    private static void DecodeInterpolatedChannel(ReadOnlySpan<byte> block, Span<byte> palette, out ulong indices)
    {
        var alpha0 = block[0];
        var alpha1 = block[1];
        palette[0] = alpha0;
        palette[1] = alpha1;
        if (alpha0 > alpha1)
        {
            for (var i = 1; i <= 6; i++)
                palette[i + 1] = (byte)(((7 - i) * alpha0 + i * alpha1) / 7);
        }
        else
        {
            for (var i = 1; i <= 4; i++)
                palette[i + 1] = (byte)(((5 - i) * alpha0 + i * alpha1) / 5);
            palette[6] = 0;
            palette[7] = 255;
        }
        indices = (ulong)BinaryPrimitives.ReadUInt32LittleEndian(block.Slice(2, 4)) |
                  ((ulong)BinaryPrimitives.ReadUInt16LittleEndian(block.Slice(6, 2)) << 32);
    }

    private static void DecodeRgb(ReadOnlySpan<byte> data, ReadOnlySpan<byte> header, uint flags,
        int width, int height, DdsPixelFormat format, byte[] output)
    {
        var bits = format == DdsPixelFormat.Rgb24 ? 24 : 32;
        var bytesPerPixel = bits / 8;
        var rowBytes = checked(width * bytesPerPixel);
        var pitch = (flags & PitchFlag) != 0 && ReadUInt32(header, 20) != 0
            ? checked((int)ReadUInt32(header, 20))
            : rowBytes;
        var rMask = ReadUInt32(header, 92);
        var gMask = ReadUInt32(header, 96);
        var bMask = ReadUInt32(header, 100);
        var aMask = ReadUInt32(header, 104);
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var offset = y * pitch + x * bytesPerPixel;
            uint packed = data[offset] | ((uint)data[offset + 1] << 8);
            packed |= (uint)data[offset + 2] << 16;
            if (bytesPerPixel == 4)
                packed |= (uint)data[offset + 3] << 24;
            var pixel = new RgbaPixel(Extract(packed, rMask), Extract(packed, gMask), Extract(packed, bMask),
                aMask == 0 ? (byte)255 : Extract(packed, aMask));
            WritePixel(output, width, x, y, pixel);
        }
    }

    private static void ValidateMasks(ReadOnlySpan<byte> header, uint bitCount)
    {
        var red = ReadUInt32(header, 92);
        var green = ReadUInt32(header, 96);
        var blue = ReadUInt32(header, 100);
        var alpha = ReadUInt32(header, 104);
        var validMask = bitCount == 32 ? uint.MaxValue : (1u << (int)bitCount) - 1;
        if (red == 0 || green == 0 || blue == 0 || ((red | green | blue | alpha) & ~validMask) != 0)
            throw new FormatException("DDS RGB channel masks are empty or exceed the declared pixel bit depth.");
        if ((red & green) != 0 || (red & blue) != 0 || (green & blue) != 0 || (alpha & (red | green | blue)) != 0)
            throw new FormatException("DDS RGB channel masks overlap.");
        foreach (var mask in new[] { red, green, blue, alpha })
        {
            if (mask == 0)
                continue;
            var shifted = mask >> BitOperations.TrailingZeroCount(mask);
            if ((shifted & (shifted + 1)) != 0)
                throw new FormatException("DDS channel masks must use contiguous bit fields.");
        }
    }

    private static byte Extract(uint packed, uint mask)
    {
        if (mask == 0)
            return 255;
        var shift = BitOperations.TrailingZeroCount(mask);
        var maximum = mask >> shift;
        var value = (packed & mask) >> shift;
        return (byte)(((ulong)value * 255 + maximum / 2) / maximum);
    }

    private static RgbaPixel Expand565(ushort value, byte alpha)
    {
        var red5 = (value >> 11) & 31;
        var green6 = (value >> 5) & 63;
        var blue5 = value & 31;
        return new RgbaPixel((byte)((red5 << 3) | (red5 >> 2)),
            (byte)((green6 << 2) | (green6 >> 4)), (byte)((blue5 << 3) | (blue5 >> 2)), alpha);
    }

    private static RgbaPixel Interpolate(RgbaPixel first, RgbaPixel second, int firstWeight, int secondWeight, int divisor) =>
        new((byte)((first.R * firstWeight + second.R * secondWeight) / divisor),
            (byte)((first.G * firstWeight + second.G * secondWeight) / divisor),
            (byte)((first.B * firstWeight + second.B * secondWeight) / divisor), 255);

    private static void WritePixel(byte[] output, int width, int x, int y, RgbaPixel pixel)
    {
        var offset = checked((y * width + x) * 4);
        output[offset] = pixel.R;
        output[offset + 1] = pixel.G;
        output[offset + 2] = pixel.B;
        output[offset + 3] = pixel.A;
    }

    private static uint ReadUInt32(ReadOnlySpan<byte> bytes, int offset) =>
        BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(offset, sizeof(uint)));

    private static bool LooksLikeCompactBioware(ReadOnlySpan<byte> bytes) =>
        bytes.Length >= 20 && ReadUInt32(bytes, 0) is > 0 and <= 16384 &&
        ReadUInt32(bytes, 4) is > 0 and <= 16384;

    private static void NormalizeRows(byte[] pixels, int width, int height, DdsStoredRowOrder selected, DdsStoredRowOrder formatDefault)
    {
        var order = selected == DdsStoredRowOrder.FormatDefault ? formatDefault : selected;
        if (order != DdsStoredRowOrder.BottomUp || height < 2) return;
        var rowBytes = checked(width * 4);
        var scratch = new byte[rowBytes];
        for (var y = 0; y < height / 2; y++)
        {
            var top = pixels.AsSpan(y * rowBytes, rowBytes);
            var bottom = pixels.AsSpan((height - 1 - y) * rowBytes, rowBytes);
            top.CopyTo(scratch);
            bottom.CopyTo(top);
            scratch.CopyTo(bottom);
        }
    }

    private static void ValidateOptions(DdsDecodeOptions options)
    {
        options.Validate();
        if (options.MaximumMipLevels <= 0)
            throw new ArgumentOutOfRangeException(nameof(options), "DDS decode limits must be positive.");
        if (!Enum.IsDefined(options.StoredRowOrder))
            throw new ArgumentOutOfRangeException(nameof(options), "DDS stored row order is unknown.");
    }
}
