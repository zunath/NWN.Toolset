using System.Buffers.Binary;

namespace Nwn.Preview.Tests.Tga;

internal static class TgaFixtures
{
    public static byte[] Image(int width, int height, byte imageType, byte depth, byte descriptor, byte[] payload)
    {
        var bytes = new byte[18 + payload.Length];
        bytes[2] = imageType;
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(12, 2), (ushort)width);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(14, 2), (ushort)height);
        bytes[16] = depth;
        bytes[17] = descriptor;
        payload.CopyTo(bytes, 18);
        return bytes;
    }
}
