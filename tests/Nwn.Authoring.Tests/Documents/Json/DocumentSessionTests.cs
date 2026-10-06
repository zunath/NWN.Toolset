using System.Text;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Documents;
using Nwn.Authoring.Documents.Json;

namespace Nwn.Authoring.Tests.Documents.Json;

[TestClass]
public sealed class DocumentSessionTests
{
    [TestMethod]
    public void CleanSave_PreservesOriginalBytesExactly()
    {
        using var temporary = new TemporaryDirectory();
        var path = Path.Combine(temporary.Path, "source.json");
        var original = Encoding.UTF8.GetBytes("{\r\n  \"unknown\" : [ 1, true, null ]\r\n}\r\n");
        File.WriteAllBytes(path, original);
        var session = DocumentSession<JsonDomDocument>.Open(path, new JsonDomCodec());

        session.Save();

        CollectionAssert.AreEqual(original, File.ReadAllBytes(path));
        Assert.IsFalse(session.IsDirty);
    }

    [TestMethod]
    public void EditUndoRedo_TracksOnlyMutationsAndDetachesAllExposedValues()
    {
        using var temporary = new TemporaryDirectory();
        var path = Path.Combine(temporary.Path, "source.json");
        File.WriteAllText(path, "{\"answer\":42,\"unknown\":{\"nested\":true}}");
        var session = DocumentSession<JsonDomDocument>.Open(path, new JsonDomCodec());
        JsonNode? retained = null;

        session.Edit(document => document.Root!["answer"] = 42);
        Assert.IsFalse(session.IsDirty);
        Assert.IsFalse(session.CanUndo);

        session.Edit(document =>
        {
            retained = document.Root;
            document.Root!["answer"] = 43;
        });
        retained!["answer"] = 99;
        Assert.AreEqual(43, session.Inspect(document => (int)document.Root!["answer"]!));
        Assert.IsTrue(session.IsDirty);
        Assert.IsTrue(session.Undo());
        Assert.IsFalse(session.IsDirty);
        Assert.IsTrue(session.Redo());
        Assert.AreEqual(43, session.Inspect(document => (int)document.Root!["answer"]!));

        session.Edit(document => document.Root!["answer"] = 44);
        Assert.IsFalse(session.CanRedo);
        Assert.AreEqual(true, session.Inspect(document => (bool)document.Root!["unknown"]!["nested"]!));
    }

    [TestMethod]
    public void ExternalChange_ConflictsWithoutLosingUnsavedEdit()
    {
        using var temporary = new TemporaryDirectory();
        var path = Path.Combine(temporary.Path, "source.json");
        File.WriteAllText(path, "{\"value\":1}");
        var session = DocumentSession<JsonDomDocument>.Open(path, new JsonDomCodec());
        session.Edit(document => document.Root!["value"] = 2);
        var external = Encoding.UTF8.GetBytes("{\"value\":3}");
        File.WriteAllBytes(path, external);

        Assert.ThrowsExactly<DocumentConflictException>(session.Save);

        Assert.IsTrue(session.IsDirty);
        Assert.AreEqual(2, session.Inspect(document => (int)document.Root!["value"]!));
        CollectionAssert.AreEqual(external, File.ReadAllBytes(path));
    }

    [TestMethod]
    public void FailedAtomicWrite_LeavesPriorFileAndSessionDirty()
    {
        using var temporary = new TemporaryDirectory();
        var path = Path.Combine(temporary.Path, "source.json");
        var original = Encoding.UTF8.GetBytes("{\"value\":1}");
        File.WriteAllBytes(path, original);
        var session = DocumentSession<JsonDomDocument>.Open(path, new JsonDomCodec(), new FailingStorage());
        session.Edit(document => document.Root!["value"] = 2);

        Assert.ThrowsExactly<IOException>(session.Save);

        Assert.IsTrue(session.IsDirty);
        CollectionAssert.AreEqual(original, File.ReadAllBytes(path));
    }

    [TestMethod]
    public void FileStorage_ConcurrentSessionsSerializeAndRejectTheLosingBaseline()
    {
        using var temporary = new TemporaryDirectory();
        var path = Path.Combine(temporary.Path, "source.json");
        File.WriteAllText(path, "{\"value\":1}");
        var storage = new SaveBarrierStorage();
        var first = DocumentSession<JsonDomDocument>.Open(path, new JsonDomCodec(), storage);
        var second = DocumentSession<JsonDomDocument>.Open(path, new JsonDomCodec(), storage);
        first.Edit(document => document.Root!["value"] = 2);
        second.Edit(document => document.Root!["value"] = 3);

        var firstSave = Task.Run(() => TrySave(first));
        var secondSave = Task.Run(() => TrySave(second));
        Task.WaitAll([firstSave, secondSave]);

        Assert.AreNotEqual(firstSave.Result, secondSave.Result);
        var diskValue = (int)JsonNode.Parse(File.ReadAllBytes(path))!["value"]!;
        Assert.IsTrue(diskValue is 2 or 3);
        Assert.AreEqual(firstSave.Result, !first.IsDirty);
        Assert.AreEqual(secondSave.Result, !second.IsDirty);
        Assert.IsTrue(first.Inspect(document => (int)document.Root!["value"]!) is 2 or 3);
        Assert.IsTrue(second.Inspect(document => (int)document.Root!["value"]!) is 2 or 3);
    }

    [TestMethod]
    public void FileStorage_SuccessfullyReplacesFileAndMovesTheSavedBaseline()
    {
        using var temporary = new TemporaryDirectory();
        var path = Path.Combine(temporary.Path, "source.json");
        File.WriteAllText(path, "{\"value\":1}");
        var session = DocumentSession<JsonDomDocument>.Open(path, new JsonDomCodec());
        var openedHash = session.PersistedFileHash;
        session.Edit(document => document.Root!["value"] = 2);

        session.Save();

        Assert.AreEqual(2, (int)JsonNode.Parse(File.ReadAllBytes(path))!["value"]!);
        Assert.IsFalse(session.IsDirty);
        Assert.AreNotEqual(openedHash, session.PersistedFileHash);
        Assert.AreEqual(Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path))), session.PersistedFileHash);
        Assert.IsTrue(session.Undo());
        Assert.IsTrue(session.IsDirty);
    }

    [TestMethod]
    public void HistoryBudget_EvictsOldestSnapshotsByStepAndByteLimits()
    {
        using var temporary = new TemporaryDirectory();
        var path = Path.Combine(temporary.Path, "source.json");
        File.WriteAllText(path, "{\"value\":0}");
        var codec = new JsonDomCodec();
        var boundedSteps = DocumentSession<JsonDomDocument>.Open(path, codec,
            options: new DocumentSessionOptions { MaximumHistorySteps = 2, MaximumHistoryBytes = 1024 });
        for (var value = 1; value <= 4; value++)
        {
            var captured = value;
            boundedSteps.Edit(document => document.Root!["value"] = captured);
        }

        Assert.IsTrue(boundedSteps.Undo());
        Assert.AreEqual(3, boundedSteps.Inspect(document => (int)document.Root!["value"]!));
        Assert.IsTrue(boundedSteps.Undo());
        Assert.AreEqual(2, boundedSteps.Inspect(document => (int)document.Root!["value"]!));
        Assert.IsFalse(boundedSteps.CanUndo);

        var boundedBytes = DocumentSession<JsonDomDocument>.Open(path, codec,
            options: new DocumentSessionOptions { MaximumHistorySteps = 10, MaximumHistoryBytes = 1 });
        boundedBytes.Edit(document => document.Root!["value"] = 7);
        Assert.IsFalse(boundedBytes.CanUndo);
        Assert.IsTrue(boundedBytes.IsDirty);
    }

    [TestMethod]
    public void HistoryBudget_AppliesAfterUndoWhenCurrentSnapshotExceedsByteLimit()
    {
        using var temporary = new TemporaryDirectory();
        var path = Path.Combine(temporary.Path, "source.json");
        File.WriteAllText(path, "{\"value\":\"x\"}");
        var session = DocumentSession<JsonDomDocument>.Open(path, new JsonDomCodec(),
            options: new DocumentSessionOptions { MaximumHistorySteps = 4, MaximumHistoryBytes = 32 });
        var largeValue = new string('a', 128);
        session.Edit(document => document.Root!["value"] = largeValue);
        Assert.IsTrue(session.CanUndo);

        Assert.IsTrue(session.Undo());

        Assert.AreEqual("x", session.Inspect(document => (string)document.Root!["value"]!));
        Assert.IsFalse(session.CanRedo);
        Assert.IsFalse(session.IsDirty);
    }

    [TestMethod]
    public void FileStorage_RejectsStaleHashAndCleansTemporaryFile()
    {
        using var temporary = new TemporaryDirectory();
        var path = Path.Combine(temporary.Path, "source.json");
        var original = Encoding.UTF8.GetBytes("old");
        File.WriteAllBytes(path, original);

        Assert.ThrowsExactly<DocumentConflictException>(() => new FileDocumentStorage()
            .WriteAtomically(path, "new"u8.ToArray(), "stale-hash"));

        CollectionAssert.AreEqual(original, File.ReadAllBytes(path));
        CollectionAssert.AreEqual(new[] { path }, Directory.GetFiles(temporary.Path));
    }

    private sealed class FailingStorage : IDocumentStorage
    {
        private readonly FileDocumentStorage _inner = new();
        public byte[]? ReadIfExists(string path) => _inner.ReadIfExists(path);
        public void WriteAtomically(string path, ReadOnlyMemory<byte> bytes, string? expectedCurrentHash) =>
            throw new IOException("Synthetic storage failure.");
    }

    private sealed class SaveBarrierStorage : IDocumentStorage
    {
        private readonly FileDocumentStorage _inner = new();
        private readonly Barrier _savePrecheckBarrier = new(2);
        private int _readCount;

        public byte[]? ReadIfExists(string path)
        {
            var readCount = Interlocked.Increment(ref _readCount);
            if (readCount is 3 or 4 && !_savePrecheckBarrier.SignalAndWait(TimeSpan.FromSeconds(5)))
                throw new TimeoutException("Both save prechecks did not reach the barrier.");
            return _inner.ReadIfExists(path);
        }

        public void WriteAtomically(string path, ReadOnlyMemory<byte> bytes, string? expectedCurrentHash) =>
            _inner.WriteAtomically(path, bytes, expectedCurrentHash);
    }

    private static bool TrySave(DocumentSession<JsonDomDocument> session)
    {
        try
        {
            session.Save();
            return true;
        }
        catch (DocumentConflictException)
        {
            return false;
        }
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        public TemporaryDirectory() => Directory.CreateDirectory(Path);
        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
