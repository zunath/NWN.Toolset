namespace Nwn.Authoring.Resources;

/// <summary>Metadata for a resolved resource with a bounded read deferred until requested.</summary>
public sealed class ResourceResolutionHandle
{
    private readonly Func<long, byte[]> _read;

    public ResourceIdentity Identity { get; }
    public ResourceProvenance Winner { get; }
    public IReadOnlyList<ResourceProvenance> Overridden { get; }

    internal ResourceResolutionHandle(
        ResourceIdentity identity,
        Func<long, byte[]> read,
        ResourceProvenance winner,
        IEnumerable<ResourceProvenance> overridden)
    {
        Identity = identity;
        _read = read;
        Winner = winner;
        Overridden = Array.AsReadOnly(overridden.ToArray());
    }

    /// <summary>Reads the selected payload only if its size is within the caller's limit.</summary>
    public byte[] ReadBytes(long maximumBytes = long.MaxValue)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maximumBytes);
        return _read(maximumBytes);
    }
}
