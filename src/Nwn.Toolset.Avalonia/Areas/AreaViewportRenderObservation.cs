using Nwn.Preview.Areas;

namespace Nwn.Toolset.Avalonia.Areas;

/// <summary>
/// Identifies the area scene and OpenGL context used by a viewport frame that completed drawing
/// and framebuffer composition.
/// </summary>
/// <param name="Scene">The exact immutable scene reference passed through the completed draw.</param>
/// <param name="SceneVersion">The viewport scene version captured for the draw.</param>
/// <param name="FrameNumber">The viewport frame stamp captured for the draw.</param>
/// <param name="OpenGlVendor">The vendor string read from the active render context.</param>
/// <param name="OpenGlRenderer">The renderer string read from the active render context.</param>
/// <param name="OpenGlVersion">The version string read from the active render context.</param>
public sealed record AreaViewportRenderObservation(
    AreaScene Scene,
    long SceneVersion,
    long FrameNumber,
    string OpenGlVendor,
    string OpenGlRenderer,
    string OpenGlVersion);
