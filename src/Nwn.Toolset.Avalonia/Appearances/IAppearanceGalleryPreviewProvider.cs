using Avalonia.Media.Imaging;

namespace Nwn.Toolset.Avalonia.Appearances;

/// <summary>Supplies host-rendered images without transferring their lifetime to the gallery.</summary>
public interface IAppearanceGalleryPreviewProvider
{
    Bitmap? Cached(AppearanceGalleryOption option);

    bool Request(
        AppearanceGalleryOption option,
        Action<Bitmap> onReady,
        Action onFailed,
        AppearanceGalleryPreviewPriority priority);
}
