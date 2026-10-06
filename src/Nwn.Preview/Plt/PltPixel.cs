namespace Nwn.Preview.Plt;

/// <summary>One PLT texel's palette lookup coordinate and opaque layer identifier.</summary>
public readonly record struct PltPixel(byte Shade, byte LayerId);
