namespace Nwn.Formats.Mdl;

/// <summary>Input and geometry limits for ASCII MDL reading.</summary>
public sealed record MdlAsciiReadOptions
{
    public int MaximumInputBytes { get; init; } = 16 * 1024 * 1024;
    public int MaximumNodeCount { get; init; } = 4096;
    public int MaximumVertexCount { get; init; } = 1_000_000;
    public int MaximumFaceCount { get; init; } = 2_000_000;

    internal void Validate()
    {
        if (MaximumInputBytes <= 0 || MaximumNodeCount <= 0 || MaximumVertexCount <= 0 || MaximumFaceCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(MdlAsciiReadOptions), "ASCII MDL read limits must be positive.");
    }
}
