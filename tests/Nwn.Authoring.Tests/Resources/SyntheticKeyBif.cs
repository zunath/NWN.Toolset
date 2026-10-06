using System.Text;

namespace Nwn.Authoring.Tests.Resources;

internal static class SyntheticKeyBif
{
    public static byte[] BuildBif(IReadOnlyList<SyntheticKeyResource> resources)
    {
        const int headerSize = 20;
        const int entrySize = 16;
        var dataStart = headerSize + resources.Count * entrySize;
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);
        writer.Write("BIFF"u8);
        writer.Write("V1  "u8);
        writer.Write((uint)resources.Count);
        writer.Write(0u);
        writer.Write((uint)headerSize);
        var offset = dataStart;
        for (var i = 0; i < resources.Count; i++)
        {
            var resource = resources[i];
            writer.Write((uint)i);
            writer.Write((uint)offset);
            writer.Write((uint)resource.Bytes.Length);
            writer.Write((uint)resource.TypeCode);
            offset += resource.Bytes.Length;
        }
        foreach (var resource in resources)
            writer.Write(resource.Bytes);
        return stream.ToArray();
    }

    public static byte[] BuildKey(string bifName, IReadOnlyList<SyntheticKeyResource> resources)
    {
        const int headerSize = 64;
        const int fileEntrySize = 12;
        const int resourceEntrySize = 22;
        var fileTableOffset = headerSize;
        var keyTableOffset = fileTableOffset + fileEntrySize;
        var filenameOffset = keyTableOffset + resources.Count * resourceEntrySize;
        var filename = Encoding.ASCII.GetBytes(bifName);
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);
        writer.Write("KEY "u8);
        writer.Write("V1  "u8);
        writer.Write(1u);
        writer.Write((uint)resources.Count);
        writer.Write((uint)fileTableOffset);
        writer.Write((uint)keyTableOffset);
        writer.Write(0u);
        writer.Write(0u);
        writer.Write(new byte[32]);
        writer.Write(0u);
        writer.Write((uint)filenameOffset);
        writer.Write((ushort)filename.Length);
        writer.Write((ushort)0);
        for (var i = 0; i < resources.Count; i++)
        {
            var name = Encoding.ASCII.GetBytes(resources[i].Resref);
            if (name.Length > 16)
                throw new ArgumentException("Synthetic KEY resrefs must fit the 16-byte field.");
            writer.Write(name);
            writer.Write(new byte[16 - name.Length]);
            writer.Write(resources[i].TypeCode);
            writer.Write((uint)i);
        }
        writer.Write(filename);
        return stream.ToArray();
    }
}
