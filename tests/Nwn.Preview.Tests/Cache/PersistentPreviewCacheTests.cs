using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Preview.Cache;

namespace Nwn.Preview.Tests.Cache;

[TestClass]
public sealed class PersistentPreviewCacheTests
{
    [TestMethod]
    public void Read_ReusesOnlyMatchingRenderAndInputFingerprint()
    {
        var root = TemporaryRoot();
        try
        {
            var cache = new PersistentPreviewCache(root, "renderer-v1", 1024);
            cache.Write("area:entry", "resolved-assets-a", [1, 2, 3], hasImage: true);
            Assert.IsTrue(cache.TryRead("area:entry", "resolved-assets-a", out var bytes, out var hasImage));
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, bytes);
            Assert.IsTrue(hasImage);
            Assert.IsFalse(cache.TryRead("area:entry", "resolved-assets-b", out _, out _));
            Assert.IsFalse(new PersistentPreviewCache(root, "renderer-v2", 1024)
                .TryRead("area:entry", "resolved-assets-a", out _, out _));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [TestMethod]
    public void Write_PersistsNoArtworkMarkerAndEnforcesDiskBudget()
    {
        var root = TemporaryRoot();
        try
        {
            var cache = new PersistentPreviewCache(root, "renderer-v1", 180);
            cache.Write("none", "fingerprint", [], hasImage: false);
            Assert.IsTrue(cache.TryRead("none", "fingerprint", out var empty, out var hasImage));
            Assert.AreEqual(0, empty!.Length);
            Assert.IsFalse(hasImage);

            cache.Write("first", "fingerprint", new byte[80], hasImage: true);
            cache.Write("second", "fingerprint", new byte[80], hasImage: true);
            Assert.IsTrue(cache.TryRead("second", "fingerprint", out _, out _));
            Assert.IsFalse(cache.TryRead("first", "fingerprint", out _, out _),
                "the disk budget evicts older entries while retaining the newest usable preview");
            var cacheFiles = Directory.GetFiles(cache.RootPath, "*.bin").Sum(FileLen);
            Assert.IsTrue(cacheFiles <= 180);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [TestMethod]
    public void SharedBudget_EvictsAcrossWorkspaceRootsWithoutClearingOtherWorkspace()
    {
        var root = TemporaryRoot();
        try
        {
            var budget = Path.Combine(root, "all-workspaces");
            var first = new PersistentPreviewCache(Path.Combine(budget, "one"), "r1", 150, budget);
            var second = new PersistentPreviewCache(Path.Combine(budget, "two"), "r1", 150, budget);
            first.Write("entry", "fingerprint", new byte[80], hasImage: true);
            second.Write("entry", "fingerprint", new byte[80], hasImage: true);

            Assert.IsTrue(second.TryRead("entry", "fingerprint", out _, out _));
            Assert.IsFalse(first.TryRead("entry", "fingerprint", out _, out _));
            Assert.IsTrue(Directory.GetFiles(budget, "*.bin", SearchOption.AllDirectories)
                .Sum(FileLen) <= 150);
            second.Clear();
            Assert.AreEqual(0, Directory.GetFiles(Path.Combine(budget, "one"), "*.bin").Length);
        }
        finally { Directory.Delete(root, recursive: true); }
    }
    [TestMethod]
    public void Read_RemovesCorruptEntryAndAllowsFreshWrite()
    {
        var root = TemporaryRoot();
        try
        {
            var cache = new PersistentPreviewCache(root, "renderer-v1", 1024);
            cache.Write("entry", "fingerprint", [4, 5], hasImage: true);
            var path = Directory.GetFiles(cache.RootPath, "*.bin").Single();
            File.WriteAllBytes(path, [0, 1, 2]);
            Assert.IsFalse(cache.TryRead("entry", "fingerprint", out _, out _));
            cache.Write("entry", "fingerprint", [7], hasImage: true);
            Assert.IsTrue(cache.TryRead("entry", "fingerprint", out var bytes, out _));
            CollectionAssert.AreEqual(new byte[] { 7 }, bytes);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [TestMethod]
    public void Contains_RemovesPayloadWithValidHeaderButInvalidHashAndAllowsFreshWrite()
    {
        var root = TemporaryRoot();
        try
        {
            var cache = new PersistentPreviewCache(root, "renderer-v1", 1024);
            cache.Write("entry", "fingerprint", [4, 5, 6, 7], hasImage: true);
            var path = Directory.GetFiles(cache.RootPath, "*.bin").Single();
            var bytes = File.ReadAllBytes(path);
            bytes[^1] ^= 0xff;
            File.WriteAllBytes(path, bytes);

            Assert.IsFalse(cache.Contains("entry", "fingerprint"),
                "Contains must validate payload integrity even when the cache header and length are intact.");
            Assert.IsFalse(File.Exists(path), "The corrupt cache entry must be removed.");

            cache.Write("entry", "fingerprint", [8, 9], hasImage: true);
            Assert.IsTrue(cache.Contains("entry", "fingerprint"));
            Assert.IsTrue(cache.TryRead("entry", "fingerprint", out var repaired, out _));
            CollectionAssert.AreEqual(new byte[] { 8, 9 }, repaired);
        }
        finally { Directory.Delete(root, recursive: true); }
    }
    [TestMethod]
    public void Contains_ReadOnlyCacheEntryRemainsAHit()
    {
        var root = TemporaryRoot();
        try
        {
            var cache = new PersistentPreviewCache(root, "renderer-v1", 1024);
            cache.Write("entry", "fingerprint", [3, 1, 4], hasImage: true);
            var path = Directory.GetFiles(cache.RootPath, "*.bin").Single();
            File.SetAttributes(path, FileAttributes.ReadOnly);

            Assert.IsTrue(cache.Contains("entry", "fingerprint"),
                "A valid read-only cache entry remains usable when access-time metadata cannot be updated.");
        }
        finally
        {
            foreach (var path in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
                File.SetAttributes(path, FileAttributes.Normal);
            Directory.Delete(root, recursive: true);
        }
    }
    [TestMethod]
    public void ReadWriteAndClear_DoNotFollowWorkspaceReparsePoint()
    {
        var root = TemporaryRoot();
        var target = Path.Combine(root, "external-target");
        var linkedRoot = Path.Combine(root, "workspace-link");
        Directory.CreateDirectory(target);
        PersistentPreviewCache? targetCache = null;
        try
        {
            targetCache = new PersistentPreviewCache(target, "renderer-v1", 1024);
            targetCache.Write("entry", "fingerprint", [9, 8, 7], hasImage: true);
            if (!OperatingSystem.IsWindows())
            {
                Directory.Delete(root, recursive: true);
                Assert.Inconclusive("The reparse-point traversal test uses a Windows directory junction.");
                return;
            }

            using var junction = Process.Start(new ProcessStartInfo("cmd.exe", $"/c mklink /J \"{linkedRoot}\" \"{target}\"")
            { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true });
            if (junction is null || !junction.WaitForExit(5000) || junction.ExitCode != 0)
            {
                Directory.Delete(root, recursive: true);
                Assert.Inconclusive("The test host cannot create a directory junction.");
                return;
            }
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or PlatformNotSupportedException or IOException)
        {
            Directory.Delete(root, recursive: true);
            Assert.Inconclusive("The test host cannot create a directory reparse point: " + exception.GetType().Name);
            return;
        }

        try
        {
            var linkedCache = new PersistentPreviewCache(linkedRoot, "renderer-v1", 1024);
            Assert.IsFalse(linkedCache.TryRead("entry", "fingerprint", out _, out _));
            Assert.AreEqual(0, linkedCache.Clear());
            linkedCache.Write("entry", "fingerprint", [1], hasImage: true);

            Assert.IsTrue(targetCache!.TryRead("entry", "fingerprint", out var bytes, out _));
            CollectionAssert.AreEqual(new byte[] { 9, 8, 7 }, bytes);
        }
        finally
        {
            if ((File.GetAttributes(linkedRoot) & FileAttributes.ReparsePoint) != 0)
                Directory.Delete(linkedRoot);
            Directory.Delete(root, recursive: true);
        }
    }

    private static string TemporaryRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "nwn-preview-disk-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static long FileLen(string path) => new FileInfo(path).Length;
}





