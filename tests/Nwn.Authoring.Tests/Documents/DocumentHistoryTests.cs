using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Documents;
using Nwn.Authoring.Documents.Json;

namespace Nwn.Authoring.Tests.Documents;

[TestClass]
public sealed class DocumentHistoryTests
{
    [TestMethod]
    public void ConstructorInspectEditAndReplace_KeepMutableValuesDetached()
    {
        var source = new JsonDomDocument(System.Text.Json.Nodes.JsonNode.Parse("{\"value\":1}"));
        var history = new DocumentHistory<JsonDomDocument>(source, new JsonDomCodec());
        source.Root!["value"] = 90;
        var inspected = history.Inspect(document => document);
        inspected.Root!["value"] = 80;
        JsonDomDocument? retainedEdit = null;

        history.Edit(document =>
        {
            retainedEdit = document;
            document.Root!["value"] = 2;
        });
        retainedEdit!.Root!["value"] = 70;
        var replacement = new JsonDomDocument(System.Text.Json.Nodes.JsonNode.Parse("{\"value\":3}"));
        history.Replace(replacement);
        replacement.Root!["value"] = 60;

        Assert.AreEqual(3, history.Inspect(document => (int)document.Root!["value"]!));
    }

    [TestMethod]
    public void UndoRedoAndMarkSaved_TrackTheAcknowledgedBaseline()
    {
        var history = new DocumentHistory<JsonDomDocument>(
            new JsonDomDocument(System.Text.Json.Nodes.JsonNode.Parse("{\"value\":1}")), new JsonDomCodec());

        history.Edit(document => document.Root!["value"] = 2);
        Assert.IsTrue(history.IsDirty);
        Assert.IsTrue(history.Undo());
        Assert.IsFalse(history.IsDirty);
        Assert.IsTrue(history.Redo());
        Assert.IsTrue(history.IsDirty);

        history.MarkSaved();
        Assert.IsFalse(history.IsDirty);
        history.Edit(document => document.Root!["value"] = 3);
        Assert.IsTrue(history.IsDirty);
        Assert.IsTrue(history.Undo());
        Assert.IsFalse(history.IsDirty);
        Assert.IsTrue(history.Redo());
        Assert.IsTrue(history.IsDirty);
    }

    [TestMethod]
    public void NewHistory_IsDirtyUntilPersistenceIsAcknowledged()
    {
        var history = new DocumentHistory<JsonDomDocument>(
            new JsonDomDocument(System.Text.Json.Nodes.JsonNode.Parse("{\"value\":1}")),
            new JsonDomCodec(),
            hasSavedBaseline: false);

        Assert.IsTrue(history.IsDirty);
        Assert.IsFalse(history.HasSavedBaseline);

        history.MarkSaved();

        Assert.IsFalse(history.IsDirty);
        Assert.IsTrue(history.HasSavedBaseline);
    }

    [TestMethod]
    public void MarkSaved_UsesTheAcknowledgedSnapshotWhileNewerEditsRemainDirty()
    {
        var history = new DocumentHistory<JsonDomDocument>(
            new JsonDomDocument(System.Text.Json.Nodes.JsonNode.Parse("{\"value\":1}")), new JsonDomCodec());
        history.Edit(document => document.Root!["value"] = 2);
        var savedDocument = history.Inspect(document => document);
        history.Edit(document => document.Root!["value"] = 3);

        history.MarkSaved(savedDocument);

        Assert.IsTrue(history.IsDirty);
        Assert.AreEqual(3, history.Inspect(document => (int)document.Root!["value"]!));
        Assert.IsTrue(history.Undo());
        Assert.IsFalse(history.IsDirty);
        Assert.AreEqual(2, history.Inspect(document => (int)document.Root!["value"]!));
    }

    [TestMethod]
    public void Constructor_DetachesCodecReturnedSnapshotArrays()
    {
        var codec = new ReusedBufferCodec();
        var history = new DocumentHistory<JsonDomDocument>(
            new JsonDomDocument(System.Text.Json.Nodes.JsonNode.Parse("{\"value\":1}")), codec);

        codec.Encode(new JsonDomDocument(System.Text.Json.Nodes.JsonNode.Parse("{\"value\":9}")));

        Assert.IsFalse(history.IsDirty);
        Assert.AreEqual(1, history.Inspect(document => (int)document.Root!["value"]!));
    }

    [TestMethod]
    public void HistoryBudget_EvictsOldSnapshotsByStepAndByteLimits()
    {
        var codec = new JsonDomCodec();
        var boundedSteps = new DocumentHistory<JsonDomDocument>(
            new JsonDomDocument(System.Text.Json.Nodes.JsonNode.Parse("{\"value\":0}")),
            codec,
            new DocumentSessionOptions { MaximumHistorySteps = 1, MaximumHistoryBytes = 1024 });
        boundedSteps.Edit(document => document.Root!["value"] = 1);
        boundedSteps.Edit(document => document.Root!["value"] = 2);
        Assert.IsTrue(boundedSteps.CanUndo);
        Assert.IsTrue(boundedSteps.Undo());
        Assert.AreEqual(1, boundedSteps.Inspect(document => (int)document.Root!["value"]!));
        Assert.IsFalse(boundedSteps.CanUndo);

        var boundedBytes = new DocumentHistory<JsonDomDocument>(
            new JsonDomDocument(System.Text.Json.Nodes.JsonNode.Parse("{\"value\":0}")),
            codec,
            new DocumentSessionOptions { MaximumHistorySteps = 10, MaximumHistoryBytes = 1 });
        boundedBytes.Edit(document => document.Root!["value"] = 1);
        Assert.IsFalse(boundedBytes.CanUndo);
    }

    private sealed class ReusedBufferCodec : IDocumentCodec<JsonDomDocument>
    {
        private readonly byte[] _buffer = new byte[11];

        public JsonDomDocument Decode(ReadOnlyMemory<byte> bytes) =>
            new(System.Text.Json.Nodes.JsonNode.Parse(bytes.Span));

        public byte[] Encode(JsonDomDocument document)
        {
            var encoded = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(document.Root);
            if (encoded.Length != _buffer.Length)
                throw new InvalidOperationException("The fixture only supports same-length values.");
            encoded.CopyTo(_buffer, 0);
            return _buffer;
        }
    }
}
