using Nwn.Authoring.Editing;

namespace Nwn.Authoring.Tests.Editing;

[TestClass]
public sealed class AtomicFileGroupWriterTests
{
    [TestMethod]
    public void CommitAllReplacesAGroupAndRemovesTransactionDebris()
    {
        using var directory = new TemporaryDirectory();
        var first = Path.Combine(directory.Path, "area.are");
        var second = Path.Combine(directory.Path, "area.git");
        File.WriteAllText(first, "old area");
        File.WriteAllText(second, "old instances");
        var writer = new AtomicFileGroupWriter(new UnrestrictedFileWriteAccess());
        var staged = new[]
        {
            writer.Stage(first, "new area"u8.ToArray()),
            writer.Stage(second, "new instances"u8.ToArray())
        };

        writer.CommitAll(staged);

        Assert.AreEqual("new area", File.ReadAllText(first));
        Assert.AreEqual("new instances", File.ReadAllText(second));
        Assert.IsFalse(Directory.EnumerateFiles(directory.Path, "*.save-backup", SearchOption.AllDirectories).Any());
        Assert.IsFalse(Directory.EnumerateFiles(directory.Path, "*.save-transaction.json", SearchOption.AllDirectories).Any());
    }

    [TestMethod]
    public void NewTargetConflictPreservesTargetAndCleansStagedFile()
    {
        using var directory = new TemporaryDirectory();
        var target = Path.Combine(directory.Path, "new.gic");
        var writer = new AtomicFileGroupWriter(new UnrestrictedFileWriteAccess());
        var staged = writer.StageNew(target, "staged"u8.ToArray());
        File.WriteAllText(target, "created concurrently");

        Assert.ThrowsExactly<IOException>(() => writer.CommitAll([staged]));

        Assert.AreEqual("created concurrently", File.ReadAllText(target));
        Assert.IsFalse(File.Exists(staged.TemporaryPath));
    }

    private sealed class UnrestrictedFileWriteAccess : IFileWriteAccess
    {
        public void EnsureAllowed() { }

        public IDisposable Acquire(string path, TimeSpan? timeout = null) => EmptyLease.Instance;
    }

    private sealed class EmptyLease : IDisposable
    {
        public static EmptyLease Instance { get; } = new();

        public void Dispose() { }
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "atomic-file-group-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
