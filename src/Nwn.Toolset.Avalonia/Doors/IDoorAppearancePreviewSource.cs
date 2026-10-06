using Nwn.Toolset.Avalonia.Appearances;

namespace Nwn.Toolset.Avalonia.Doors;

/// <summary>Renders the door appearance gallery's thumbnails through the host's model cache.</summary>
public interface IDoorAppearancePreviewSource
{
    /// <summary>
    /// A preview provider for <paramref name="entries"/>, each a door appearance with the option id its
    /// gallery tile carries; the provider receives those ids back as the options it is asked to draw.
    /// Null shows the gallery without pictures.
    /// </summary>
    IAppearanceGalleryPreviewProvider? Create(IReadOnlyList<DoorAppearanceGalleryEntry> entries);
}
