using System.Buffers.Binary;

namespace Nwn.Preview.Tests.Plt;

internal static class PltFixtures
{
    public static byte[] Image(int width, int height, byte[] pixels)
    {
        var bytes = new byte[24 + pixels.Length];
        "PLT "u8.CopyTo(bytes);
        "V1  "u8.CopyTo(bytes.AsSpan(4));
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(16, 4), (uint)width);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(20, 4), (uint)height);
        pixels.CopyTo(bytes, 24);
        return bytes;
    }
}
