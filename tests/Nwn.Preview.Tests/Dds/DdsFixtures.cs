using System.Buffers.Binary;
using System.Text;

namespace Nwn.Preview.Tests.Dds;

internal static class DdsFixtures
{
    public static byte[] CompactBioWare(int width, int height, byte formatCode, byte[] payload, uint? baseSize = null)
    {
        var bytes = new byte[20 + payload.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0, 4), (uint)width);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4, 4), (uint)height);
        bytes[8] = formatCode;
        var blockBytes = formatCode == 3 ? 8 : 16;
        var expectedBaseSize = (uint)(((width + 3) / 4) * ((height + 3) / 4) * blockBytes);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12, 4), baseSize ?? expectedBaseSize);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(16, 4), BitConverter.SingleToInt32Bits(1));
        payload.CopyTo(bytes, 20);
        return bytes;
    }

    public static byte[] Compressed(int width, int height, string fourCc, byte[] payload, uint mipCount = 1)
    {
        var bytes = Header(width, height, mipCount);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8, 4), 0x000A1007);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(20, 4), (uint)payload.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(80, 4), 0x4);
        Encoding.ASCII.GetBytes(fourCc).CopyTo(bytes, 84);
        return bytes.Concat(payload).ToArray();
    }

    public static byte[] Rgb32(byte[] pixel, uint red, uint green, uint blue, uint alpha)
    {
        var bytes = Header(1, 1, 1);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8, 4), 0x100F);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(20, 4), 4);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(80, 4), 0x40);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(88, 4), 32);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(92, 4), red);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(96, 4), green);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(100, 4), blue);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(104, 4), alpha);
        return bytes.Concat(pixel).ToArray();
    }

    public static byte[] Rgb24(byte[] pixel, uint red, uint green, uint blue)
    {
        var bytes = Header(1, 1, 1);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8, 4), 0x100F);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(20, 4), 3);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(80, 4), 0x40);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(88, 4), 24);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(92, 4), red);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(96, 4), green);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(100, 4), blue);
        return bytes.Concat(pixel).ToArray();
    }

    public static byte[] Header(int width, int height, uint mipCount)
    {
        var bytes = new byte[128];
        Encoding.ASCII.GetBytes("DDS ").CopyTo(bytes, 0);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4, 4), 124);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8, 4), 0x1007);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12, 4), (uint)height);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(16, 4), (uint)width);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(28, 4), mipCount);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(76, 4), 32);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(108, 4), 0x1000);
        return bytes;
    }
}
