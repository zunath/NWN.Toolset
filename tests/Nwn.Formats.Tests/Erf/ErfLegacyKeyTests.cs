using System.Buffers.Binary;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Formats.Erf;
using Nwn.Formats.Resources;

namespace Nwn.Formats.Tests.Erf;

[TestClass]
public sealed class ErfLegacyKeyTests
{
    [TestMethod]
    [DataRow("iprp_spells past")]
    [DataRow("ife_actions_&_tr")]
    [DataRow("ife_arm's_length")]
    [DataRow("udp1-blck-gr2")]
    [DataRow("legacy.name~")]
    [DataRow("LEGACY-NAME")]
    public void ImportedKeyAndPayloadSurviveStreamingRepackWhileAuthoredNamesStayCanonical(string name)
    {
        byte[] payload = [1, 7, 25, 255];
        using var original = new MemoryStream();
        ErfWriter.Write(original, ErfContainerKind.Hak,
            [ErfResourceSource.FromBytes(Resref.Parse("original"), ResourceType.TwoDa, payload),
             ErfResourceSource.FromBytes(Resref.Parse("xm_valid"), ResourceType.Utc, [9])],
            ErfBuildDate.ContentEpoch);
        var bytes = original.ToArray();
        var keyOffset = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(24)));
        bytes.AsSpan(keyOffset, Resref.MaxLength).Clear();
        Encoding.ASCII.GetBytes(name).CopyTo(bytes, keyOffset);
        using var imported = ErfArchive.Read(new MemoryStream(bytes));
        var legacy = imported.Entries.Single(entry => entry.ResRef.Value == name.ToLowerInvariant());

        Assert.AreEqual(2, imported.Entries.Count);
        CollectionAssert.AreEqual(payload, imported.ReadAllBytes(legacy));
        Assert.IsFalse(Resref.TryParse(name, out _, out _));
        using var repacked = new MemoryStream();
        ErfWriter.Write(repacked, ErfContainerKind.Hak,
            imported.Entries.Select(entry => new ErfResourceSource(entry.ResRef, entry.Type, entry.Size,
                () => imported.OpenResourceStream(entry))).ToArray(), ErfBuildDate.ContentEpoch);
        repacked.Position = 0;
        using var reopened = ErfArchive.Read(repacked, ownsStream: false);
        var preserved = reopened.Find(legacy.ResRef, ResourceType.TwoDa);
        Assert.IsNotNull(preserved);
        CollectionAssert.AreEqual(payload, reopened.ReadAllBytes(preserved));
        Assert.AreEqual(2, reopened.Entries.Count);
        CollectionAssert.AreEqual(new byte[] { 9 }, reopened.ReadAllBytes(reopened.Find(Resref.Parse("xm_valid"), ResourceType.Utc)!));
    }

    [TestMethod]
    public void LegacyKeyDoesNotBypassResourceRangeValidation()
    {
        using var original = new MemoryStream();
        ErfWriter.Write(original, ErfContainerKind.Hak,
            [ErfResourceSource.FromBytes(Resref.Parse("original"), ResourceType.TwoDa, [1])], ErfBuildDate.ContentEpoch);
        var bytes = original.ToArray();
        var keyOffset = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(24)));
        bytes.AsSpan(keyOffset, Resref.MaxLength).Clear();
        Encoding.ASCII.GetBytes("legacy-name").CopyTo(bytes, keyOffset);
        var resourceOffset = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(28)));
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(resourceOffset), uint.MaxValue);

        Assert.ThrowsExactly<FormatException>(() => ErfArchive.Read(new MemoryStream(bytes)));
    }
}
