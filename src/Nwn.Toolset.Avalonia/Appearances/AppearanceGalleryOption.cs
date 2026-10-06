namespace Nwn.Toolset.Avalonia.Appearances;

/// <summary>Host-neutral display information for one appearance choice.</summary>
public sealed record AppearanceGalleryOption(
    AppearanceGalleryOptionId Id,
    string Caption,
    string? Detail = null)
{
    public string SearchText { get; } = $"{Caption} {Detail}".ToLowerInvariant();
}
