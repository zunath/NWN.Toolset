using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Formats.Key;

using Nwn.Formats.Resources;

namespace Nwn.Formats.Tests.Key;

[TestClass]
public sealed class StockArchiveTests
{
    [TestMethod]
    public void StreamSourceRejectsAnOversizePayloadBeforeLoadingItAndDisposesEachRead()
    {
        const int payloadSize = 8 * 1024 * 1024;
        var built = KeyBifFixture.Build([new Resource("xm_large", 2017, "data/one.bif", new byte[payloadSize])]);
        var key = KeyReader.Read(built.KeyBytes);
        var streams = new List<CountingStream>();
        var archive = StockArchive.FromStreams(key, name =>
        {
            var stream = new CountingStream(built.BifBytesByFilename[name]);
            streams.Add(stream);
            return stream;
        });
        var entry = key.Find(Resref.Parse("xm_large"), ResourceType.TwoDa)!;
        var before = GC.GetAllocatedBytesForCurrentThread();

        Assert.ThrowsExactly<FormatException>(() => archive.ReadResource(entry, 1024));

        Assert.IsTrue(GC.GetAllocatedBytesForCurrentThread() - before < 1024 * 1024);
        Assert.AreEqual(36L, streams.Single().BytesRead);
        Assert.IsTrue(streams.Single().Disposed);
        Assert.AreEqual(64L, archive.CachedBifBytes);
        Assert.AreEqual(payloadSize, archive.ReadResource(entry, payloadSize).Length);
        Assert.AreEqual(payloadSize, streams[1].BytesRead);
        Assert.IsTrue(streams[1].Disposed);
    }

    [TestMethod]
    public void StreamSourceLimitsRetainedIndexesAcrossBifsAndRefusesChangedArchiveLength()
    {
        var built = KeyBifFixture.Build([
            new Resource("xm_a", 2017, "data/one.bif", [1]),
            new Resource("xm_b", 2017, "data/two.bif", [2]),
        ]);
        var key = KeyReader.Read(built.KeyBytes);
        var sources = built.BifBytesByFilename.ToDictionary(pair => pair.Key, pair => pair.Value);
        var archive = StockArchive.FromStreams(key, name => new MemoryStream(sources[name]), 64);
        var first = key.Find(Resref.Parse("xm_a"), ResourceType.TwoDa)!;
        var second = key.Find(Resref.Parse("xm_b"), ResourceType.TwoDa)!;

        CollectionAssert.AreEqual(new byte[] { 1 }, archive.ReadResource(first));
        Assert.ThrowsExactly<FormatException>(() => archive.ReadResource(second));
        Assert.AreEqual(64L, archive.CachedBifBytes);
        sources["data/one.bif"] = sources["data/one.bif"][..^1];
        Assert.ThrowsExactly<FormatException>(() => archive.ReadResource(first));
    }

    [TestMethod]
    public void ReadResource_LoadsOnlyTheBifTheResourceIsIn()
    {
        var built = KeyBifFixture.Build([
            new Resource("placeables", 2017, "data/small.bif", "2DA V2.0\n\nLabel\n"u8.ToArray()),
            new Resource("xm_never_touched", 2033, "data/huge.bif", [9, 9, 9]),
        ]);
        var key = KeyReader.Read(built.KeyBytes);

        var loadedBifs = new List<string>();
        var archive = new StockArchive(key, name =>
        {
            loadedBifs.Add(name);
            return built.BifBytesByFilename[name];
        });

        var bytes = archive.ReadResource(Resref.Parse("placeables"), ResourceType.TwoDa);
        CollectionAssert.AreEqual("2DA V2.0\n\nLabel\n"u8.ToArray(), bytes);
        CollectionAssert.AreEqual(new[] { "data/small.bif" }, loadedBifs);
    }

    [TestMethod]
    public void ReadResource_CachesEachBifOnlyOnce()
    {
        var built = KeyBifFixture.Build([
            new Resource("xm_a", 2017, "data/one.bif", [1]),
            new Resource("xm_b", 2027, "data/one.bif", [2]),
        ]);
        var key = KeyReader.Read(built.KeyBytes);
        var loadCount = 0;
        var archive = new StockArchive(key, name => { loadCount++; return built.BifBytesByFilename[name]; });

        archive.ReadResource(Resref.Parse("xm_a"), ResourceType.TwoDa);
        archive.ReadResource(Resref.Parse("xm_b"), ResourceType.Utc);
        Assert.AreEqual(1, loadCount);
    }

    [TestMethod]
    public void ReadResource_ConcurrentReadsShareOneBoundedBifLoad()
    {
        var built = KeyBifFixture.Build([new Resource("xm_a", 2017, "data/one.bif", [1, 2, 3])]);
        var key = KeyReader.Read(built.KeyBytes);
        var loadCount = 0;
        var archive = new StockArchive(key, name =>
        {
            Interlocked.Increment(ref loadCount);
            Thread.Sleep(10);
            return built.BifBytesByFilename[name];
        });
        var entry = key.Find(Resref.Parse("xm_a"), ResourceType.TwoDa)!;

        Parallel.For(0, 16, _ => CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, archive.ReadResource(entry)));

        Assert.AreEqual(1, loadCount);
    }

    [TestMethod]
    public void ReadResource_UnknownResource_Throws()
    {
        var built = KeyBifFixture.Build([new Resource("xm_a", 2017, "data/one.bif", [1])]);
        var key = KeyReader.Read(built.KeyBytes);
        var archive = new StockArchive(key, name => built.BifBytesByFilename[name]);
        Assert.ThrowsExactly<KeyNotFoundException>(() => archive.ReadResource(Resref.Parse("xm_missing"), ResourceType.TwoDa));
    }

    [TestMethod]
    public void BifsNeededFor_ListsDistinctBifsWithoutLoadingThem()
    {
        var built = KeyBifFixture.Build([
            new Resource("xm_a", 2017, "data/one.bif", [1]),
            new Resource("xm_b", 2017, "data/two.bif", [2]),
            new Resource("xm_c", 2027, "data/one.bif", [3]),
        ]);
        var key = KeyReader.Read(built.KeyBytes);
        var archive = new StockArchive(key, _ => throw new InvalidOperationException("Should not load any BIF."));

        var needed = archive.BifsNeededFor([
            (Resref.Parse("xm_a"), ResourceType.TwoDa),
            (Resref.Parse("xm_b"), ResourceType.TwoDa),
            (Resref.Parse("xm_c"), ResourceType.Utc),
        ]);
        CollectionAssert.AreEquivalent(new[] { "data/one.bif", "data/two.bif" }, needed.ToList());
    }

    private sealed class CountingStream(byte[] bytes) : Stream
    {
        private readonly MemoryStream _inner = new(bytes, writable: false);
        public long BytesRead { get; private set; }
        public bool Disposed { get; private set; }
        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => _inner.CanSeek;
        public override bool CanWrite => false;
        public override long Length => _inner.Length;
        public override long Position { get => _inner.Position; set => _inner.Position = value; }
        public override void Flush() => _inner.Flush();
        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

        public override int Read(Span<byte> buffer)
        {
            var count = _inner.Read(buffer);
            BytesRead += count;
            return count;
        }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            if (disposing) _inner.Dispose();
            base.Dispose(disposing);
        }
    }
}
