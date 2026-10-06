using Avalonia.Media.Imaging;
using Nwn.Authoring.Behaviors;

namespace Nwn.Toolset.Avalonia.Behaviors;

/// <summary>The host resolves resource artwork and publishes completed pictures on the UI thread.</summary>
public interface IBehaviorChoicePreviewProvider
{
    Bitmap? Cached(BehaviorChoice choice, int maxWidth, bool cropTransparentCanvas = false);

    Task RequestAsync(BehaviorChoice choice, int maxWidth, Action<Bitmap> onReady,
        bool cropTransparentCanvas = false);
}
