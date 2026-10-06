using Nwn.Preview.Dds;
using Nwn.Preview.Pixels;

namespace Nwn.Preview.Textures;

/// <summary>Host-selected palette inputs and decoding limits for one texture request.</summary>
public sealed class TextureLoadRequest
{
    public IReadOnlyDictionary<byte, int> PaletteRows { get; init; } = new Dictionary<byte, int>();
    public Func<byte, RgbaImage?>? ResolvePalette { get; init; }
    public DdsStoredRowOrder StandardDdsRowOrder { get; init; } = DdsStoredRowOrder.FormatDefault;
    public RasterDecodeOptions RasterOptions { get; init; } = new();
    public int MaximumCompressedBytes { get; init; } = 512 * 1024 * 1024;

    internal void Validate()
    {
        RasterOptions.Validate();
        if (MaximumCompressedBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(MaximumCompressedBytes));
    }
}
