namespace Nwn.Authoring.Resources;

/// <summary>A winning resource and the lower-priority or same-layer entries it shadows.</summary>
public sealed class ResourceResolution
{
    private readonly byte[] _bytes;
    public ResourceIdentity Identity { get; }
    public byte[] Bytes => _bytes.ToArray();
    public ResourceProvenance Winner { get; }
    public IReadOnlyList<ResourceProvenance> Overridden { get; }

    internal ResourceResolution(
        ResourceIdentity identity,
        byte[] bytes,
        ResourceProvenance winner,
        IEnumerable<ResourceProvenance> overridden)
    {
        Identity = identity;
        _bytes = bytes.ToArray();
        Winner = winner;
        Overridden = Array.AsReadOnly(overridden.ToArray());
    }
}
