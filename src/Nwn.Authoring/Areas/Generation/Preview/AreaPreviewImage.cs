#nullable disable
using Nwn.Authoring.Areas.Generation.Tilesets;

namespace Nwn.Authoring.Areas.Generation.Preview
{
    /// <summary>Top-left, row-major RGBA preview pixels.</summary>
    public sealed record AreaPreviewImage(int Width, int Height, byte[] Pixels, int MissingTileGraphics);
}
