using Nwn.Preview.Pixels;

namespace Nwn.Preview.Textures;

public sealed record TextureLoadResult(RgbaImage Image, TextureSourceFormat SourceFormat, float? CompactAlphaMean);
