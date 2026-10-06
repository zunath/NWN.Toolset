namespace Nwn.Preview.Thumbnails;

/// <summary>A host-owned distinction that affects decoded texture identity for one mesh.</summary>
public readonly record struct ThumbnailTextureCacheVariant
{
    public string Value { get; } = string.Empty;

    public ThumbnailTextureCacheVariant(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Value = value;
    }

    internal string ToCacheKeySuffix() => string.IsNullOrEmpty(Value)
        ? string.Empty
        : $"|host:{Value.Length}:{Value}";
}