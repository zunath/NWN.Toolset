using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;
using Nwn.Formats.Mdl;
using Nwn.Preview.Cache;
using Nwn.Preview.Pixels;
using Nwn.Preview.Scene;
using Nwn.Preview.Tests.Scene;

namespace Nwn.Preview.Tests.Cache;

[TestClass]
public sealed class PreviewAssetCacheTests
{
    [TestMethod]
    public void Cache_ReusesPreparedGeometryWithoutRebuildingTheScene()
    {
        var source = MdlAsciiReader.Read(Encoding.ASCII.GetBytes(MdlSceneFixtures.StaticMeshModel(withAnimation: false)));
        var cache = new PreviewAssetCache(maximumEntries: 2, maximumBytes: 128);
        var createCount = 0;
        var first = cache.GetOrAddScene("native-model", () =>
        {
            createCount++;
            return MdlScenePreparer.Prepare(source);
        });
        var repeated = cache.GetOrAddScene("native-model", () => throw new AssertFailedException("A cache hit must not prepare again."));

        Assert.AreSame(first, repeated);
        Assert.AreEqual(1, createCount);
        Assert.AreEqual(128, cache.CachedBytes);
    }

    [TestMethod]
    public void Cache_ReusesDecodedImagesAndEvictsLeastRecentWithinBothBudgets()
    {
        var cache = new PreviewAssetCache(maximumEntries: 2, maximumBytes: 12);
        var first = cache.GetOrAddImage("first", () => new RgbaImage(1, 1, [1, 2, 3, 4]));
        Assert.AreSame(first, cache.GetOrAddImage("first", () => throw new AssertFailedException("A cache hit must not decode again.")));
        _ = cache.GetOrAddImage("second", () => new RgbaImage(1, 1, [5, 6, 7, 8]));
        _ = cache.GetOrAddImage("third", () => new RgbaImage(2, 1, [9, 10, 11, 12, 13, 14, 15, 16]));

        Assert.AreEqual(2, cache.CachedEntryCount);
        Assert.AreEqual(12, cache.CachedBytes);
        var reloaded = cache.GetOrAddImage("first", () => new RgbaImage(1, 1, [0, 0, 0, 0]));
        Assert.AreNotSame(first, reloaded);
        Assert.AreEqual(2, cache.CachedEntryCount);
        Assert.AreEqual(12, cache.CachedBytes);
    }

    [TestMethod]
    public void Cache_DoesNotRetainAnAssetLargerThanItsWholeBudget()
    {
        var cache = new PreviewAssetCache(maximumEntries: 4, maximumBytes: 4);
        var image = cache.GetOrAddImage("large", () => new RgbaImage(2, 1, [1, 2, 3, 4, 5, 6, 7, 8]));

        Assert.AreEqual(2, image.Width);
        Assert.AreEqual(0, cache.CachedEntryCount);
        Assert.AreEqual(0, cache.CachedBytes);
    }
}
