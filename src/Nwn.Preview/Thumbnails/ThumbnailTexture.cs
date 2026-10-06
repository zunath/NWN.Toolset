namespace Nwn.Preview.Thumbnails;

/// <summary>RGBA texture pixels consumed by the software thumbnail renderer.</summary>
/// <remarks>Pixels are top-down, row-major RGBA8, with an alpha cutoff for transparent texel handling.</remarks>
public sealed record ThumbnailTexture(int Width, int Height, byte[] Pixels, byte AlphaCutoff = ThumbnailTexture.DefaultAlphaCutoff)
{
    public const byte DefaultAlphaCutoff = 96;
}