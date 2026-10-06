using System.Buffers.Binary;
using Nwn.Formats.Resources;
using Nwn.Preview.Dds;
using Nwn.Preview.Pixels;
using Nwn.Preview.Plt;
using Nwn.Preview.Tga;

namespace Nwn.Preview.Textures;

/// <summary>Loads a resource's TGA, DDS, or PLT texture using caller-supplied bytes and palette policy.</summary>
public static class TextureResourceLoader
{
    public static TextureLoadResult? Load(
        string resref,
        Func<string, ResourceType, byte[]?> readResource,
        TextureLoadRequest? request = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resref);
        ArgumentNullException.ThrowIfNull(readResource);
        request ??= new TextureLoadRequest();
        request.Validate();

        var tga = readResource(resref, ResourceType.Tga);
        if (tga is not null)
            return new TextureLoadResult(TgaDecoder.Decode(CheckSize(tga, request), request.RasterOptions), TextureSourceFormat.Tga, null);

        var dds = readResource(resref, ResourceType.Dds);
        if (dds is not null)
        {
            CheckSize(dds, request);
            var standard = IsStandardDds(dds);
            var image = DdsDecoder.Decode(dds, new DdsDecodeOptions
            {
                StoredRowOrder = standard ? request.StandardDdsRowOrder : DdsStoredRowOrder.FormatDefault,
                MaximumWidth = request.RasterOptions.MaximumWidth,
                MaximumHeight = request.RasterOptions.MaximumHeight,
                MaximumPixels = request.RasterOptions.MaximumPixels
            });
            float? alphaMean = standard || dds.Length < 20
                ? null
                : BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(dds.AsSpan(16, 4)));
            return new TextureLoadResult(image, TextureSourceFormat.Dds, alphaMean);
        }

        var pltBytes = readResource(resref, ResourceType.Plt);
        return pltBytes is null ? null : LoadPlt(pltBytes, request);
    }

    public static TextureLoadResult LoadPlt(byte[] pltBytes, TextureLoadRequest request)
    {
        ArgumentNullException.ThrowIfNull(pltBytes);
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();
        CheckSize(pltBytes, request);
        var texture = PltDecoder.Decode(pltBytes, request.RasterOptions);
        var layerIds = new HashSet<byte>();
        var sourcePixels = texture.CopyPixelBytes();
        for (var offset = 0; offset < sourcePixels.Length; offset += 2)
            layerIds.Add(sourcePixels[offset + 1]);

        var palettes = new Dictionary<byte, RgbaImage>();
        var tints = new Dictionary<byte, PltLayerTint>();
        foreach (var layerId in layerIds)
        {
            var palette = request.ResolvePalette?.Invoke(layerId);
            if (palette is null)
                continue;
            palettes.Add(layerId, palette);
            var row = request.PaletteRows.TryGetValue(layerId, out var selected) ? selected : 0;
            row = Math.Clamp(row, 0, palette.Height - 1);
            tints.Add(layerId, new PltLayerTint(row));
        }

        if (palettes.Count == 0)
            return new TextureLoadResult(Grayscale(texture, sourcePixels), TextureSourceFormat.Plt, null);
        foreach (var layerId in layerIds)
        {
            if (palettes.ContainsKey(layerId))
                continue;
            // The host's deterministic fallback for a missing palette is grayscale per texel.
            return new TextureLoadResult(Grayscale(texture, sourcePixels, palettes, tints), TextureSourceFormat.Plt, null);
        }

        return new TextureLoadResult(PltCompositor.Compose(texture, palettes, tints, request.RasterOptions), TextureSourceFormat.Plt, null);
    }

    private static byte[] CheckSize(byte[] bytes, TextureLoadRequest request)
    {
        if (bytes.Length > request.MaximumCompressedBytes)
            throw new FormatException("Texture resource exceeds the configured compressed-input limit.");
        return bytes;
    }

    private static bool IsStandardDds(byte[] bytes) => bytes.Length >= 4 && bytes.AsSpan(0, 4).SequenceEqual("DDS "u8);

    private static RgbaImage Grayscale(PltTexture texture, byte[] sourcePixels,
        IReadOnlyDictionary<byte, RgbaImage>? palettes = null, IReadOnlyDictionary<byte, PltLayerTint>? tints = null)
    {
        var output = new byte[checked(texture.Width * texture.Height * 4)];
        for (var pixel = 0; pixel < sourcePixels.Length / 2; pixel++)
        {
            var shade = sourcePixels[pixel * 2];
            var layer = sourcePixels[pixel * 2 + 1];
            var offset = pixel * 4;
            if (palettes?.TryGetValue(layer, out var palette) == true && tints?.TryGetValue(layer, out var tint) == true)
            {
                var color = palette.GetPixel(shade, tint.PaletteRow);
                output[offset] = color.R;
                output[offset + 1] = color.G;
                output[offset + 2] = color.B;
                output[offset + 3] = color.A;
            }
            else
            {
                output[offset] = shade;
                output[offset + 1] = shade;
                output[offset + 2] = shade;
                output[offset + 3] = shade == 0 ? (byte)0 : (byte)255;
            }
        }
        return new RgbaImage(texture.Width, texture.Height, output);
    }
}
