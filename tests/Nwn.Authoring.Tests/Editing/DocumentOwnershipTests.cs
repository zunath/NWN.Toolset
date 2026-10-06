using System.Text;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Editing;

namespace Nwn.Authoring.Tests.Editing;

[TestClass]
public sealed class DocumentOwnershipTests
{
    [TestMethod]
    public async Task SessionBoundAfterAwaitGuardsLaterCallerMutations()
    {
        var document = CreateDocument("before");
        using var session = await BindAfterAwait(document);
        var field = document.Root.Get("Tag");

        Assert.ThrowsExactly<InvalidOperationException>(() => field.SetString("outside"));
        session.Execute("Change tag", () => field.SetString("inside"));

        Assert.AreEqual("inside", field.GetString());
    }

    [TestMethod]
    public async Task CrossContextDisposeReleasesOnlyItsOwnDocumentRegistration()
    {
        var firstDocument = CreateDocument("first");
        var secondDocument = CreateDocument("second");
        var first = new DocumentSession("first.are", firstDocument);
        using var second = new DocumentSession("second.are", secondDocument);

        await Task.Run(first.Dispose);

        firstDocument.Root.Get("Tag").SetString("released");
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            secondDocument.Root.Get("Tag").SetString("still-owned"));
    }

    [TestMethod]
    public async Task DisposeWaitsForOpenTransactionBeforeReleasingOwnership()
    {
        var document = CreateDocument("before");
        var session = new DocumentSession("active.are", document);
        using var transaction = session.Begin("Change tag");
        var disposeStarted = new ManualResetEventSlim();
        Task disposeTask;
        using (ExecutionContext.SuppressFlow())
        {
            disposeTask = Task.Run(() =>
            {
                disposeStarted.Set();
                session.Dispose();
            });
        }

        Assert.IsTrue(disposeStarted.Wait(TimeSpan.FromSeconds(5)));
        Assert.IsFalse(disposeTask.IsCompleted);
        document.Root.Get("Tag").SetString("inside");
        transaction.Commit();
        await disposeTask.WaitAsync(TimeSpan.FromSeconds(5));

        document.Root.Get("Tag").SetString("after-dispose");
        Assert.AreEqual("after-dispose", document.Root.Get("Tag").GetString());
    }

    [TestMethod]
    public void OwningSessionCannotBeDisposedInsideItsOpenTransaction()
    {
        var document = CreateDocument("before");
        using var session = new DocumentSession("active.are", document);
        using var transaction = session.Begin("Change tag");

        Assert.ThrowsExactly<InvalidOperationException>(session.Dispose);
        document.Root.Get("Tag").SetString("inside");
        transaction.Commit();

        Assert.ThrowsExactly<InvalidOperationException>(() => document.Root.Get("Tag").SetString("outside"));
    }

    [TestMethod]
    public void TransactionRejectsMutationOfAnotherOwnedDocumentAndRollsBack()
    {
        var firstDocument = CreateDocument("first");
        var secondDocument = CreateDocument("second");
        using var first = new DocumentSession("first.are", firstDocument);
        using var second = new DocumentSession("second.are", secondDocument);

        Assert.ThrowsExactly<InvalidOperationException>(() => first.Execute("Wrong owner", () =>
        {
            firstDocument.Root.Get("Tag").SetString("rolled-back");
            secondDocument.Root.Get("Tag").SetString("rejected");
        }));

        Assert.AreEqual("first", firstDocument.Root.Get("Tag").GetString());
        Assert.AreEqual("second", secondDocument.Root.Get("Tag").GetString());
        Assert.AreEqual(0, first.UndoStack.Position);
    }

    [TestMethod]
    public void SessionRegistrationPropagatesToInsertedAndReplacementChildren()
    {
        var document = CreateDocument("before");
        using var session = new DocumentSession("area.are", document);
        var list = JsonGffField.CreateList();
        var child = JsonGffField.CreateStruct(0).Struct!;
        child.Add("Tag", StringField("child"));

        session.Execute("Add list", () => document.Root.Add("Items", list));
        session.Execute("Add child", () => list.InsertElement(0, child));
        Assert.ThrowsExactly<InvalidOperationException>(() => child.Get("Tag").SetString("outside"));

        var replacement = CreateDocument("replacement");
        session.ReloadFrom(replacement, replacement.ToBytes());
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            session.Document.Root.Get("Tag").SetString("outside-replacement"));

        // Stale field references remain session-owned until the session closes.
        Assert.ThrowsExactly<InvalidOperationException>(() => child.Get("Tag").SetString("stale-reference"));
    }

    [TestMethod]
    public void SharedNodeRemainsOwnedUntilItsLastSessionReleasesIt()
    {
        var document = CreateDocument("before");
        var shared = StringField("shared");
        document.Root.Add("First", shared);
        document.Root.Add("Second", shared);
        var first = new DocumentSession("first.are", document);
        var second = new DocumentSession("second.are", document);

        Assert.ThrowsExactly<InvalidOperationException>(() => shared.SetString("outside"));
        first.Dispose();
        Assert.ThrowsExactly<InvalidOperationException>(() => shared.SetString("still-owned"));
        second.Dispose();

        shared.SetString("released");
        Assert.AreEqual("released", shared.GetString());
    }

    [TestMethod]
    public void ReplacementOwnershipReleasesOldAndCurrentGraphsTogether()
    {
        var original = CreateDocument("original");
        var oldField = original.Root.Get("Tag");
        var session = new DocumentSession("replacement.are", original);
        var replacement = CreateDocument("replacement");
        var newField = replacement.Root.Get("Tag");

        session.ReloadFrom(replacement, replacement.ToBytes());

        Assert.ThrowsExactly<InvalidOperationException>(() => oldField.SetString("while-open"));
        Assert.ThrowsExactly<InvalidOperationException>(() => newField.SetString("while-open"));
        session.Dispose();

        oldField.SetString("old-released");
        newField.SetString("new-released");
        Assert.AreEqual("old-released", oldField.GetString());
        Assert.AreEqual("new-released", newField.GetString());
    }

    [TestMethod]
    public void ReplacementTraversalRegistersNewNestedDescendants()
    {
        var document = CreateDocument("before");
        using var session = new DocumentSession("replacement.are", document);
        var replacement = CreateDocument("replacement");
        var list = JsonGffField.CreateList();
        var child = JsonGffField.CreateStruct(0).Struct!;
        var childTag = StringField("child");
        child.Add("Tag", childTag);
        list.InsertElement(0, child);
        replacement.Root.Add("Items", list);

        session.ReloadFrom(replacement, replacement.ToBytes());

        Assert.ThrowsExactly<InvalidOperationException>(() => childTag.SetString("outside"));
        session.Execute("Edit descendant", () => childTag.SetString("inside"));
        Assert.AreEqual("inside", childTag.GetString());
    }

    [TestMethod]
    public void InsertedLocalizedEntriesInheritDocumentOwnership()
    {
        var document = CreateDocument("before");
        using var session = new DocumentSession("area.are", document);
        var localized = JsonGffField.CreateLocString();
        var entry = new LocStringEntry("0", JsonStringCodec.Encode("text"));

        session.Execute("Add localized field", () => document.Root.Add("Localized", localized));
        session.Execute("Add localized entry", () => localized.AddLocStringEntry(entry));

        Assert.ThrowsExactly<InvalidOperationException>(() => entry.SetText("outside"));
        session.Execute("Edit localized entry", () => entry.SetText("inside"));
        Assert.AreEqual("inside", JsonStringCodec.Decode(entry.RawText));
    }

    [TestMethod]
    public void RemovedNodeRemainsOwnedWhileUndoHistoryCanRestoreIt()
    {
        var document = CreateDocument("before");
        using var session = new DocumentSession("area.are", document);
        var field = document.Root.Get("Tag");

        session.Execute("Remove tag", () => document.Root.Remove("Tag"));

        Assert.ThrowsExactly<InvalidOperationException>(() => field.SetString("outside"));
        session.Undo();
        session.Execute("Change restored tag", () => field.SetString("inside"));
        Assert.AreEqual("inside", document.Root.Get("Tag").GetString());
    }

    [TestMethod]
    public void DroppedHistoryDoesNotKeepRemovedGraphAlive()
    {
        var document = CreateDocument("before");
        using var session = new DocumentSession("area.are", document);
        var removed = RemoveFieldAndDropHistory(session, document);
        CollectUntilDead(removed);
        Assert.IsFalse(document.Root.Contains("Tag"));
    }

    [TestMethod]
    public void ConstructionSuppressesCaptureForDetachedNodesButKeepsOwnedNodesGuarded()
    {
        var document = CreateDocument("before");
        using var session = new DocumentSession("area.are", document);
        JsonGffDocument? detached = null;

        session.Execute("Construct detached graph", () =>
        {
            using (EditScope.EnterConstruction())
            {
                detached = CreateDocument("detached");
                detached.Root.Get("Tag").SetString("constructed");
                Assert.ThrowsExactly<InvalidOperationException>(() =>
                    document.Root.Get("Tag").SetString("must-not-bypass-owner"));
            }
        });

        Assert.IsNotNull(detached);
        Assert.AreEqual("constructed", detached.Root.Get("Tag").GetString());
        Assert.AreEqual("before", document.Root.Get("Tag").GetString());
        Assert.AreEqual(0, session.UndoStack.Position);
    }

    private static async Task<DocumentSession> BindAfterAwait(JsonGffDocument document)
    {
        await Task.Yield();
        return new DocumentSession("async.are", document);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<JsonGffField> RemoveFieldAndDropHistory(
        DocumentSession session,
        JsonGffDocument document)
    {
        var field = document.Root.Get("Tag");
        var weak = new WeakReference<JsonGffField>(field);
        session.Execute("Remove tag", () => document.Root.Remove("Tag"));
        session.UndoStack.Reset();
        return weak;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CollectUntilDead(WeakReference<JsonGffField> removed)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            if (!removed.TryGetTarget(out _))
                return;
        }

        Assert.Fail("The session ownership registry must not retain nodes after their undo history is dropped.");
    }

    private static JsonGffDocument CreateDocument(string value)
    {
        var root = JsonGffField.CreateStruct(0).Struct!;
        root.Add("Tag", StringField(value));
        return new JsonGffDocument("ARE ", root);
    }

    private static JsonGffField StringField(string value) =>
        JsonGffField.CreateScalar(GffFieldType.CExoString, JsonStringCodec.Encode(value));
}
