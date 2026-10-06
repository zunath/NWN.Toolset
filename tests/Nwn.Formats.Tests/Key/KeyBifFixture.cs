using Nwn.Formats.Io;

namespace Nwn.Formats.Tests.Key;

/// <summary>Hand-builds synthetic KEY V1 / BIF V1 bytes (there is no writer -- production code only
/// ever reads a base game's own files) so <see cref="Nwn.Formats.Key.KeyReader"/> and
/// <see cref="Nwn.Formats.Key.BifReader"/> can be exercised without a real NWN install.</summary>
internal static class KeyBifFixture
{
    public static Built Build(IReadOnlyList<Resource> resources)
    {
        var bifNames = resources.Select(r => r.BifFilename).Distinct(StringComparer.Ordinal).ToList();
        var bifBytesByName = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        var resourceIndexInBif = new Dictionary<Resource, int>();

        foreach (var bifName in bifNames)
        {
            var inThisBif = resources.Where(r => r.BifFilename == bifName).ToList();
            for (var i = 0; i < inThisBif.Count; i++)
            {
                resourceIndexInBif[inThisBif[i]] = i;
            }

            bifBytesByName[bifName] = BuildBif(inThisBif);
        }

        var keyBytes = BuildKey(resources, bifNames, resourceIndexInBif);
        return new Built(keyBytes, bifBytesByName);
    }

    private static byte[] BuildBif(IReadOnlyList<Resource> resources)
    {
        const int headerSize = 20;
        const int entrySize = 16;
        var tableOffset = headerSize;
        var dataStart = tableOffset + resources.Count * entrySize;

        var writer = new BinaryFormatWriter();
        writer.WriteFixedAscii("BIFF", 4);
        writer.WriteFixedAscii("V1  ", 4);
        writer.WriteUInt32((uint)resources.Count);
        writer.WriteUInt32(0); // fixed resource count, always 0
        writer.WriteUInt32((uint)tableOffset);

        var offsets = new uint[resources.Count];
        var cursor = (uint)dataStart;
        for (var i = 0; i < resources.Count; i++)
        {
            offsets[i] = cursor;
            cursor += (uint)resources[i].Bytes.Length;
        }

        for (var i = 0; i < resources.Count; i++)
        {
            writer.WriteUInt32((uint)i); // ID -- low 20 bits are this resource's own index
            writer.WriteUInt32(offsets[i]);
            writer.WriteUInt32((uint)resources[i].Bytes.Length);
            writer.WriteUInt32(resources[i].TypeCode);
        }

        foreach (var resource in resources)
        {
            writer.WriteBytes(resource.Bytes);
        }

        return writer.ToArray();
    }

    private static byte[] BuildKey(IReadOnlyList<Resource> resources, IReadOnlyList<string> bifNamesList, IReadOnlyDictionary<Resource, int> resourceIndexInBif)
    {
        const int headerSize = 64;
        const int fileTableEntrySize = 12;
        const int keyTableEntrySize = 22;
        var bifNames = bifNamesList.ToList();

        var offsetToFileTable = headerSize;
        var offsetToKeyTable = offsetToFileTable + bifNames.Count * fileTableEntrySize;
        var filenameBlockStart = offsetToKeyTable + resources.Count * keyTableEntrySize;

        var filenameOffsets = new uint[bifNames.Count];
        var filenameSizes = new ushort[bifNames.Count];
        var cursor = (uint)filenameBlockStart;
        for (var i = 0; i < bifNames.Count; i++)
        {
            filenameOffsets[i] = cursor;
            filenameSizes[i] = (ushort)bifNames[i].Length;
            cursor += filenameSizes[i];
        }

        var writer = new BinaryFormatWriter();
        writer.WriteFixedAscii("KEY ", 4);
        writer.WriteFixedAscii("V1  ", 4);
        writer.WriteUInt32((uint)bifNames.Count);
        writer.WriteUInt32((uint)resources.Count);
        writer.WriteUInt32((uint)offsetToFileTable);
        writer.WriteUInt32((uint)offsetToKeyTable);
        writer.WriteUInt32(126); // build year, arbitrary
        writer.WriteUInt32(0);   // build day, arbitrary
        writer.WriteBytes(new byte[32]); // reserved

        for (var i = 0; i < bifNames.Count; i++)
        {
            writer.WriteUInt32(0); // file size, unused by the reader
            writer.WriteUInt32(filenameOffsets[i]);
            writer.WriteUInt16(filenameSizes[i]);
            writer.WriteUInt16(0); // "drives", obsolete/unused
        }

        foreach (var resource in resources)
        {
            var bifIndex = bifNames.IndexOf(resource.BifFilename);
            var resId = ((uint)bifIndex << 20) | (uint)resourceIndexInBif[resource];
            writer.WriteFixedAscii(resource.ResRef, 16);
            writer.WriteUInt16(resource.TypeCode);
            writer.WriteUInt32(resId);
        }

        foreach (var bifName in bifNames)
        {
            writer.WriteAscii(bifName);
        }

        return writer.ToArray();
    }
}
