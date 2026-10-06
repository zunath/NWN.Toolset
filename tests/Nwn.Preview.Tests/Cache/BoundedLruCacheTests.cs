using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Preview.Cache;

namespace Nwn.Preview.Tests.Cache;

[TestClass]
public sealed class BoundedLruCacheTests
{
    [TestMethod]
    public void GetOrAdd_EvictsLeastRecentlyUsedUntilBothBudgetsHold()
    {
        var cache = new BoundedLruCache<string, string>(4, 2, value => value?.Length ?? 0);
        cache.GetOrAdd("first", () => "aa");
        cache.GetOrAdd("second", () => "bb");
        Assert.AreEqual("aa", cache.GetOrAdd("first", () => "changed"));
        cache.GetOrAdd("third", () => "ccc");

        Assert.AreEqual(3, cache.HeldBytes);
        Assert.AreEqual("aa", cache.GetOrAdd("first", () => "aa"));
        Assert.AreEqual("ccc", cache.GetOrAdd("third", () => "ccc"));
        Assert.AreEqual("bb", cache.GetOrAdd("second", () => "bb"));
        Assert.IsTrue(cache.HeldBytes <= 4);
    }

    [TestMethod]
    public void GetOrAdd_DoesNotRetainAnEntryLargerThanTheByteBudget()
    {
        var cache = new BoundedLruCache<string, string>(2, 4, value => value?.Length ?? 0);
        Assert.AreEqual("large", cache.GetOrAdd("large", () => "large"));
        Assert.AreEqual(0, cache.HeldBytes);
        var calls = 0;
        cache.GetOrAdd("large", () => { calls++; return "large"; });
        Assert.AreEqual(1, calls);
    }
}
