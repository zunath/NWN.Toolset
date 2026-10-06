namespace Nwn.Authoring.Areas.Generation.Hosting;

/// <summary>A decoded tile picture in top-left, row-major RGBA byte order.</summary>
/// <param name="Width">Width in pixels.</param>
/// <param name="Height">Height in pixels.</param>
/// <param name="Pixels">Four bytes per pixel.</param>
public sealed record AreaPreviewTexture(int Width, int Height, byte[] Pixels);
