namespace Nwn.Toolset.Avalonia.Appearances;

/// <summary>Stable identity for a host-owned appearance choice.</summary>
public readonly record struct AppearanceGalleryOptionId
{
    public AppearanceGalleryOptionId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("An appearance option id cannot be empty.", nameof(value));
        Value = value;
    }

    public static AppearanceGalleryOptionId Unknown { get; } = new("system/unknown");

    public string Value { get; }
    public override string ToString() => Value;
}
