namespace Nwn.Authoring.Resources;

/// <summary>Bounds file-backed resources, source archive sizes and retained KEY/BIF metadata.</summary>
public sealed record ResourceLayerReadOptions
{
    public long MaximumResourceBytes { get; init; } = 256L * 1024 * 1024;
    public long MaximumKeyFileBytes { get; init; } = 64L * 1024 * 1024;
    public long MaximumBifFileBytes { get; init; } = 512L * 1024 * 1024;
    public long MaximumCachedBifBytes { get; init; } = 1024L * 1024 * 1024;

    internal void Validate()
    {
        if (MaximumResourceBytes <= 0 || MaximumKeyFileBytes <= 0 || MaximumBifFileBytes <= 0 || MaximumCachedBifBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(ResourceLayerReadOptions), "Resource read limits must be positive.");
    }
}
