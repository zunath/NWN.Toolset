namespace Nwn.Formats.Mtr;

/// <summary>Bounds applied before decoding or allocating material statements.</summary>
public sealed record MtrReadOptions
{
    public int MaximumInputBytes { get; init; } = 1024 * 1024;
    public int MaximumLineLength { get; init; } = 8192;
    public int MaximumDirectiveCount { get; init; } = 4096;

    internal void Validate()
    {
        if (MaximumInputBytes <= 0 || MaximumLineLength <= 0 || MaximumDirectiveCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(MtrReadOptions), "Material read bounds must be positive.");
    }
}
