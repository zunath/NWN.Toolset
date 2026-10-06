namespace Nwn.Authoring.Resources;

/// <summary>The typed resource identity. The engine accepts raw resref characters beyond the
/// stricter authored-name policy; names are case-normalized but otherwise preserved.</summary>
public readonly record struct ResourceIdentity
{
    public string Resref { get; }
    public Nwn.Formats.Resources.ResourceType Type { get; }

    public ResourceIdentity(string resref, Nwn.Formats.Resources.ResourceType type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resref);
        if (resref.Length > 16)
            throw new ArgumentException("A resource resref cannot exceed 16 characters.", nameof(resref));
        if (resref.Any(char.IsControl))
            throw new ArgumentException("A resource resref cannot contain control characters.", nameof(resref));
        Resref = resref.ToLowerInvariant();
        Type = type;
    }
}
