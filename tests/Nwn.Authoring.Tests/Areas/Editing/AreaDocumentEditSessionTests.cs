using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;
using Nwn.Authoring.Areas.Editing;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Editing;

namespace Nwn.Authoring.Tests.Areas.Editing;

[TestClass]
public sealed class AreaDocumentEditSessionTests
{
    [TestMethod]
    public void UnifiedHistoryUndoesAndRedoesTheLatestAreaOrInstanceEdit()
    {
        using var session = CreateSession();
        session.ExecuteArea("Change area", () => session.Area.Document.Root.Get("Tag").SetString("area"));
        Assert.AreEqual(AreaDocumentEditTarget.Area, session.LastUndoableTarget);
        session.ExecuteInstances("Change instances", () => session.Instances.Document.Root.Get("Tag").SetString("instances"));
        Assert.AreEqual(AreaDocumentEditTarget.Instances, session.LastUndoableTarget);

        Assert.AreEqual("instances", session.Instances.Document.Root.Get("Tag").GetString());
        Assert.IsTrue(session.UndoLatest());
        Assert.AreEqual(AreaDocumentEditTarget.Area, session.LastUndoableTarget);
        Assert.AreEqual(AreaDocumentEditTarget.Instances, session.LastRedoableTarget);
        Assert.AreEqual(string.Empty, session.Instances.Document.Root.Get("Tag").GetString());
        Assert.AreEqual("area", session.Area.Document.Root.Get("Tag").GetString());
        Assert.IsTrue(session.UndoLatest());
        Assert.AreEqual(string.Empty, session.Area.Document.Root.Get("Tag").GetString());

        Assert.IsTrue(session.RedoLatest());
        Assert.AreEqual("area", session.Area.Document.Root.Get("Tag").GetString());
        Assert.IsTrue(session.RedoLatest());
        Assert.AreEqual("instances", session.Instances.Document.Root.Get("Tag").GetString());
    }

    [TestMethod]
    public void InstanceAndCommentChangesUndoAsOneTransaction()
    {
        using var session = CreateSession();
        session.ExecuteInstances("Add paired instance", () =>
        {
            session.Instances.Document.Root.Get("Tag").SetString("placed");
            session.Comments.Document.Root.Get("Comment").SetString("note");
        });

        Assert.IsTrue(session.IsCommentsDirty);
        Assert.IsTrue(session.UndoInstances());
        Assert.AreEqual(string.Empty, session.Instances.Document.Root.Get("Tag").GetString());
        Assert.AreEqual(string.Empty, session.Comments.Document.Root.Get("Comment").GetString());
        Assert.IsFalse(session.IsCommentsDirty);
        Assert.IsTrue(session.RedoInstances());
        Assert.AreEqual("placed", session.Instances.Document.Root.Get("Tag").GetString());
        Assert.AreEqual("note", session.Comments.Document.Root.Get("Comment").GetString());
    }

    [TestMethod]
    public void GroupDisposeReleasesAreaInstanceAndCommentOwnershipTogether()
    {
        var session = CreateSession();
        session.ExecuteArea("Change area", () => session.Area.Document.Root.Get("Tag").SetString("area"));
        session.ExecuteInstances("Change instances and comments", () =>
        {
            session.Instances.Document.Root.Get("Tag").SetString("instances");
            session.Comments.Document.Root.Get("Comment").SetString("comment");
        });

        session.Dispose();

        session.Area.Document.Root.Get("Tag").SetString("after-dispose");
        session.Instances.Document.Root.Get("Tag").SetString("after-dispose");
        session.Comments.Document.Root.Get("Comment").SetString("after-dispose");
    }

    [TestMethod]
    public void GroupDisposeDuringOwnedTransactionLeavesGroupUsable()
    {
        var session = CreateSession();
        using var transaction = session.Instances.Begin("Change instances");

        Assert.ThrowsExactly<InvalidOperationException>(session.Dispose);
        session.Instances.Document.Root.Get("Tag").SetString("inside");
        transaction.Commit();
        Assert.IsTrue(session.ExecuteInstances(
            "Change comment",
            () => session.Comments.Document.Root.Get("Comment").SetString("note")));

        session.Dispose();
        session.Area.Document.Root.Get("Tag").SetString("after-dispose");
    }

    [TestMethod]
    public void NoOpDoesNotChangeHistoryOrDiscardRedoAndSavedCommentBaselineIsExplicit()
    {
        using var session = CreateSession();
        session.ExecuteArea("Change area", () => session.Area.Document.Root.Get("Tag").SetString("area"));
        Assert.IsTrue(session.UndoArea());
        Assert.IsFalse(session.ExecuteInstances("No-op", static () => { }));
        Assert.IsTrue(session.CanRedoArea);
        Assert.IsTrue(session.RedoArea());

        session.ExecuteInstances("Change comment", () => session.Comments.Document.Root.Get("Comment").SetString("saved"));
        Assert.IsTrue(session.IsCommentsDirty);
        session.AcceptSavedComments(session.Comments.ToBytes());
        Assert.IsFalse(session.IsCommentsDirty);
        Assert.IsTrue(session.Instances.UndoStack.IsDirty);
    }

    [TestMethod]
    public void SaveAcknowledgementUsesCapturedBytesAndLaterUndoReturnsToSavedBaseline()
    {
        using var session = CreateSession();
        session.ExecuteArea("Edit before save", () => session.Area.Document.Root.Get("Tag").SetString("captured"));
        var snapshot = session.CaptureSaveSnapshot();
        var firstByte = snapshot.AreaBytes[0];
        var detachedBytes = snapshot.AreaBytes;
        detachedBytes[0] ^= 0xff;
        Assert.AreEqual(firstByte, snapshot.AreaBytes[0]);

        session.ExecuteArea("Edit while save is pending", () => session.Area.Document.Root.Get("Tag").SetString("newer"));
        session.AcceptSaved(snapshot);

        Assert.IsTrue(session.IsDirty);
        Assert.IsTrue(session.UndoLatest());
        Assert.AreEqual("captured", session.Area.Document.Root.Get("Tag").GetString());
        Assert.IsFalse(session.IsDirty);
    }

    private static AreaDocumentEditSession CreateSession()
    {
        static JsonGffDocument Create(string dataType, string fieldName)
        {
            var root = JsonGffField.CreateStruct(0).Struct!;
            root.Add(fieldName, JsonGffField.CreateScalar(
                GffFieldType.CExoString, Encoding.UTF8.GetBytes("\"\"")));
            return new JsonGffDocument(dataType, root);
        }

        var area = Create("ARE ", "Tag");
        var instances = Create("GIT ", "Tag");
        var comments = Create("GIC ", "Comment");
        return new AreaDocumentEditSession(
            new DocumentSession("area.are", area),
            new DocumentSession("area.git", instances),
            new DocumentSession("area.gic", comments));
    }
}
