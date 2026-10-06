namespace Nwn.Preview.Pixels;

/// <summary>Dimension and allocation limits for untrusted raster inputs.</summary>
public record RasterDecodeOptions
{
    public int MaximumWidth { get; init; } = 16384;
    public int MaximumHeight { get; init; } = 16384;
    public long MaximumPixels { get; init; } = 64L * 1024 * 1024;

    internal void Validate()
    {
        if (MaximumWidth <= 0 || MaximumHeight <= 0 || MaximumPixels <= 0)
            throw new ArgumentOutOfRangeException(nameof(RasterDecodeOptions), "Raster decode limits must be positive.");
    }
}
