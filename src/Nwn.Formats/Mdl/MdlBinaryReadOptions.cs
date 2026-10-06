namespace Nwn.Formats.Mdl;

/// <summary>Bounds for compiled binary MDL geometry parsing.</summary>
public sealed record MdlBinaryReadOptions
{
    public int MaximumInputBytes { get; init; } = 64 * 1024 * 1024;
    public int MaximumNodeCount { get; init; } = 4096;
    public int MaximumHierarchyDepth { get; init; } = 128;
    public int MaximumVertexCount { get; init; } = 1_000_000;
    public int MaximumFaceCount { get; init; } = 2_000_000;

    internal void Validate()
    {
        if (MaximumInputBytes <= 0 || MaximumNodeCount <= 0 || MaximumHierarchyDepth <= 0 || MaximumHierarchyDepth > 512 ||
            MaximumVertexCount <= 0 || MaximumFaceCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(MdlBinaryReadOptions), "Binary MDL read limits must be positive.");
    }
}
