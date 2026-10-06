using Nwn.Preview.Pixels;

namespace Nwn.Preview.Dds;

/// <summary>Allocation and dimension limits for untrusted DDS input.</summary>
public sealed record DdsDecodeOptions : RasterDecodeOptions
{
    public int MaximumMipLevels { get; init; } = 16;
    public DdsStoredRowOrder StoredRowOrder { get; init; } = DdsStoredRowOrder.FormatDefault;
}
