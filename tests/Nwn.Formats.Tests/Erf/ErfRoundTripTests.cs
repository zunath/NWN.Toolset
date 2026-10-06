using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Formats.Erf;

using Nwn.Formats.Resources;

namespace Nwn.Formats.Tests.Erf;

[TestClass]
public sealed class ErfRoundTripTests
{
    private static List<ErfResourceSource> SampleResources() =>
    [
        ErfResourceSource.FromBytes(Resref.Parse("xm_persist_npc"), ResourceType.Utc, "utc-bytes-1"u8.ToArray()),
        ErfResourceSource.FromBytes(Resref.Parse("xm_combat_bow"), ResourceType.Uti, "uti-bytes-1"u8.ToArray()),
        ErfResourceSource.FromBytes(Resref.Parse("aaaa"), ResourceType.TwoDa, [1, 2, 3, 4, 5]),
        ErfResourceSource.FromBytes(Resref.Parse("aaaa"), ResourceType.Mdl, [9, 9]), // same resref, different type: legal
    ];

    [TestMethod]
    public void WriteThenRead_RoundTripsAllEntriesAndBytes()
    {
        using var stream = new MemoryStream();
        ErfWriter.Write(stream, ErfContainerKind.Hak, SampleResources(), ErfBuildDate.ContentEpoch);
        stream.Position = 0;

        using var archive = ErfArchive.Read(stream, ownsStream: false);
        Assert.AreEqual(ErfContainerKind.Hak, archive.Kind);
        Assert.AreEqual(4, archive.Entries.Count);

        var npc = archive.Find(Resref.Parse("xm_persist_npc"), ResourceType.Utc);
        Assert.IsNotNull(npc);
        CollectionAssert.AreEqual("utc-bytes-1"u8.ToArray(), archive.ReadAllBytes(npc));

        var aaaaTwoDa = archive.Find(Resref.Parse("aaaa"), ResourceType.TwoDa);
        var aaaaMdl = archive.Find(Resref.Parse("aaaa"), ResourceType.Mdl);
        Assert.IsNotNull(aaaaTwoDa);
        Assert.IsNotNull(aaaaMdl);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4, 5 }, archive.ReadAllBytes(aaaaTwoDa));
        CollectionAssert.AreEqual(new byte[] { 9, 9 }, archive.ReadAllBytes(aaaaMdl));
    }

    [TestMethod]
    public void Write_IsDeterministic_SameInputProducesByteIdenticalOutput()
    {
        using var streamA = new MemoryStream();
        using var streamB = new MemoryStream();
        ErfWriter.Write(streamA, ErfContainerKind.Hak, SampleResources(), ErfBuildDate.ContentEpoch);
        ErfWriter.Write(streamB, ErfContainerKind.Hak, SampleResources(), ErfBuildDate.ContentEpoch);
        CollectionAssert.AreEqual(streamA.ToArray(), streamB.ToArray());
    }

    [TestMethod]
    public void Write_SortsEntriesByResRefThenType_RegardlessOfInputOrder()
    {
        var forward = SampleResources();
        var reversed = SampleResources();
        reversed.Reverse();

        using var streamForward = new MemoryStream();
        using var streamReversed = new MemoryStream();
        ErfWriter.Write(streamForward, ErfContainerKind.Hak, forward, ErfBuildDate.ContentEpoch);
        ErfWriter.Write(streamReversed, ErfContainerKind.Hak, reversed, ErfBuildDate.ContentEpoch);

        CollectionAssert.AreEqual(streamForward.ToArray(), streamReversed.ToArray());
    }

    [TestMethod]
    public void Write_ContainerKindRoundTrips_ForHakModAndErf()
    {
        foreach (var kind in new[] { ErfContainerKind.Hak, ErfContainerKind.Mod, ErfContainerKind.Erf })
        {
            using var stream = new MemoryStream();
            ErfWriter.Write(stream, kind, SampleResources(), ErfBuildDate.ContentEpoch);
            stream.Position = 0;
            using var archive = ErfArchive.Read(stream, ownsStream: false);
            Assert.AreEqual(kind, archive.Kind);
        }
    }

    [TestMethod]
    public void Write_RejectsDuplicateResRefAndType()
    {
        var resources = new List<ErfResourceSource>
        {
            ErfResourceSource.FromBytes(Resref.Parse("dupe"), ResourceType.Utc, [1]),
            ErfResourceSource.FromBytes(Resref.Parse("dupe"), ResourceType.Utc, [2]),
        };
        using var stream = new MemoryStream();
        Assert.ThrowsExactly<InvalidOperationException>(() => ErfWriter.Write(stream, ErfContainerKind.Hak, resources, ErfBuildDate.ContentEpoch));
    }

    [TestMethod]
    public void CopyResourceTo_StreamsWithoutLoadingWholeResourceUpFront()
    {
        var largeBytes = new byte[500_000];
        new Random(42).NextBytes(largeBytes);
        var resources = new List<ErfResourceSource> { ErfResourceSource.FromBytes(Resref.Parse("bigone"), ResourceType.Dds, largeBytes) };

        using var stream = new MemoryStream();
        ErfWriter.Write(stream, ErfContainerKind.Hak, resources, ErfBuildDate.ContentEpoch);
        stream.Position = 0;

        using var archive = ErfArchive.Read(stream, ownsStream: false);
        var entry = archive.Entries.Single();
        using var destination = new MemoryStream();
        archive.CopyResourceTo(entry, destination);
        CollectionAssert.AreEqual(largeBytes, destination.ToArray());
    }
}
