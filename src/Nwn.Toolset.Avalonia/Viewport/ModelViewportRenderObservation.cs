using Nwn.Preview.Pixels;
using Nwn.Preview.Scene;

namespace Nwn.Toolset.Avalonia.Viewport;

/// <summary>Describes the scene and textures used by a completed model viewport draw.</summary>
public sealed record ModelViewportRenderObservation(
    PreparedScene Scene,
    IReadOnlyDictionary<string, RgbaImage> Textures,
    long FrameNumber,
    string OpenGlVendor,
    string OpenGlRenderer,
    string OpenGlVersion);
