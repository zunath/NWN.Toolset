using System.Text;
using Nwn.Authoring.Behaviors;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Editing;
using Nwn.Formats.Gff;
using Nwn.Toolset.Avalonia.Sounds;

namespace Nwn.Toolset.Avalonia.Tests.Sounds;

[TestClass]
public sealed class SoundListEditorViewModelTests
{
    [TestMethod]
    public void SearchAddRemoveAndReorderUseHostTransactionsAndPreserveEntryData()
    {
        var document = CreateDocument();
        using var session = new DocumentSession("sound-list.uts.json", document);
        var store = new BehaviorValueStore(document.Root);
        var descriptions = new List<string>();
        var changed = 0;
        var editor = new SoundListEditorViewModel(store, new("Sounds", "Sound"),
            ["wind", "rain", "birds"], 0, (description, mutation) =>
            {
                descriptions.Add(description);
                session.Execute(description, mutation);
                return true;
            }, () => changed++);

        editor.Search = "rai";
        CollectionAssert.AreEqual(new[] { "rain" }, editor.FilteredSounds.ToArray());
        editor.Candidate = "rain";
        editor.AddCommand.Execute(null);
        CollectionAssert.AreEqual(new[] { "wind", "rain", "rain" }, store.GetResRefList("Sounds", "Sound").ToArray());
        Assert.AreEqual(1, changed);

        editor.SelectedEntry = editor.Rows[2];
        editor.MoveUpCommand.Execute(null);
        CollectionAssert.AreEqual(new[] { "wind", "rain", "rain" }, store.GetResRefList("Sounds", "Sound").ToArray());
        Assert.AreEqual(2, changed);
        var nativeEntries = document.Root.Get("Sounds").Elements!;
        Assert.AreEqual("retained-first", nativeEntries[0].GetOrNull("Future")?.GetString());
        Assert.IsNull(nativeEntries[1].GetOrNull("Future")?.GetString());
        Assert.AreEqual("retained-second", nativeEntries[2].GetOrNull("Future")?.GetString());
        editor.RemoveCommand.Execute(null);
        CollectionAssert.AreEqual(new[] { "wind", "rain" }, store.GetResRefList("Sounds", "Sound").ToArray());
        Assert.AreEqual("retained-second", document.Root.Get("Sounds").Elements![1].GetOrNull("Future")?.GetString());
        session.Undo();
        CollectionAssert.AreEqual(new[] { "wind", "rain", "rain" }, store.GetResRefList("Sounds", "Sound").ToArray());
        session.Redo();
        CollectionAssert.AreEqual(new[] { "wind", "rain" }, store.GetResRefList("Sounds", "Sound").ToArray());
        Assert.AreEqual(3, descriptions.Count);
        Assert.AreEqual("Add sound", descriptions[0]);
        Assert.AreEqual("Move sound rain up", descriptions[1]);
        Assert.AreEqual("Remove sound rain", descriptions[2]);
    }

    [TestMethod]
    public void AddCreatesTheNativeSoundsListWhenItWasAbsent()
    {
        var document = JsonGffDocument.Parse(Encoding.UTF8.GetBytes("""{"__data_type":"UTS "}"""));
        var store = new BehaviorValueStore(document.Root);
        var editor = new SoundListEditorViewModel(store, new("Sounds", "Sound"), ["wind"], 0,
            (_, mutation) => { mutation(); return true; }, () => { });
        editor.Candidate = "wind";
        editor.AddCommand.Execute(null);
        CollectionAssert.AreEqual(new[] { "wind" }, store.GetResRefList("Sounds", "Sound").ToArray());
    }

    [TestMethod]
    public void SingleSoundLimitExplainsRefusalAndRemoveRestoresCapacity()
    {
        var document = JsonGffDocument.Parse(Encoding.UTF8.GetBytes("""{"__data_type":"UTS "}"""));
        var store = new BehaviorValueStore(document.Root);
        store.AddResRefListEntry("Sounds", "Sound", "wind");
        var editor = new SoundListEditorViewModel(store, new("Sounds", "Sound"), ["wind", "rain"], 1,
            (_, mutation) => { mutation(); return true; }, () => { });
        Assert.AreEqual("This behavior plays one sound. Remove one to choose another.", editor.Status);
        var before = document.ToBytes();
        editor.Candidate = "rain";
        Assert.IsFalse(editor.AddCommand.CanExecute(null));
        CollectionAssert.AreEqual(before, document.ToBytes());

        editor.SelectedEntry = editor.Rows[0];
        editor.RemoveCommand.Execute(null);
        Assert.IsTrue(editor.HasRoom);
        Assert.IsNull(editor.Status);
        Assert.IsTrue(editor.AddCommand.CanExecute(null));
        editor.AddCommand.Execute(null);
        CollectionAssert.AreEqual(new[] { "rain" }, store.GetResRefList("Sounds", "Sound").ToArray());
    }

    [TestMethod]
    public void StaleRowsRefusedTransactionsAndLateCallbacksDoNotMutateTheList()
    {
        var document = CreateDocument();
        var store = new BehaviorValueStore(document.Root);
        var editCalls = 0;
        Action? retainedMutation = null;
        var editor = new SoundListEditorViewModel(store, new("Sounds", "Sound"), ["wind", "rain"], 0,
            (_, mutation) => { editCalls++; retainedMutation = mutation; return true; }, () => { });
        var staleRow = editor.Rows[0];
        editor.Reload();
        editor.SelectedEntry = staleRow;
        editor.RemoveCommand.Execute(null);
        Assert.AreEqual(0, editCalls);
        CollectionAssert.AreEqual(new[] { "wind", "rain" }, store.GetResRefList("Sounds", "Sound").ToArray());

        editor.SelectedEntry = editor.Rows[0];
        editor.RemoveCommand.Execute(null);
        Assert.AreEqual(1, editCalls);
        retainedMutation!();
        CollectionAssert.AreEqual(new[] { "wind", "rain" }, store.GetResRefList("Sounds", "Sound").ToArray());
    }

    [TestMethod]
    public void InvalidCatalogEntriesStayBrowseableButCannotBeAddedOrChangeLegacyRows()
    {
        var document = CreateLegacyDocument();
        using var session = new DocumentSession("legacy-sound-list.uts.json", document);
        var store = new BehaviorValueStore(document.Root);
        var before = document.ToBytes();
        var editCalls = 0;
        var changed = 0;
        var changeCandidateDuringEdit = false;
        var preview = new RecordingSoundListPreview();
        SoundListEditorViewModel? editor = null;
        editor = new SoundListEditorViewModel(store, new("Sounds", "Sound"),
            ["wind", "rain", "bad-name", "name.with.dot", "as_cv_ta-da1"], 0,
            (_, mutation) =>
            {
                editCalls++;
                if (changeCandidateDuringEdit)
                    editor!.Candidate = "bad-name";
                session.Execute("Edit sound list", mutation);
                return true;
            }, () => changed++, preview: preview);

        editor.Search = "as_cv_ta-da1";
        CollectionAssert.AreEqual(new[] { "as_cv_ta-da1" }, editor.FilteredSounds.ToArray());
        editor.SelectedEntry = null;
        editor.Candidate = "as_cv_ta-da1";
        Assert.AreEqual("This sound’s resource name cannot be added to a native playlist.", editor.Status);
        Assert.IsFalse(editor.AddCommand.CanExecute(null));
        editor.AddCommand.Execute(null);
        Assert.IsTrue(editor.CanPreview);
        Assert.AreEqual("as_cv_ta-da1", editor.PreviewTarget);
        editor.PlayCommand.Execute(null);
        Assert.AreEqual("as_cv_ta-da1", preview.PlayedResRef);
        Assert.AreEqual(0, editCalls);
        CollectionAssert.AreEqual(before, document.ToBytes());
        CollectionAssert.AreEqual(new[] { "as_cv_ta-da1", "wind" }, store.GetResRefList("Sounds", "Sound").ToArray());

        changeCandidateDuringEdit = true;
        editor.Candidate = "wind";
        editor.AddCommand.Execute(null);
        Assert.AreEqual(1, editCalls);
        Assert.AreEqual(0, changed);
        CollectionAssert.AreEqual(before, document.ToBytes());

        changeCandidateDuringEdit = false;
        editor.Candidate = "rain";
        editor.AddCommand.Execute(null);
        Assert.AreEqual(2, editCalls);
        Assert.AreEqual(1, changed);
        CollectionAssert.AreEqual(new[] { "as_cv_ta-da1", "wind", "rain" }, store.GetResRefList("Sounds", "Sound").ToArray());

        var saved = NativeRoundTrip(document);
        Assert.AreEqual("as_cv_ta-da1", saved.Root.Get("Sounds").Elements![0].GetOrNull("Sound")!.GetString());
        Assert.AreEqual(17u, saved.Root.Get("Sounds").Elements![0].StructId);
        Assert.AreEqual("retained-invalid", saved.Root.Get("Sounds").Elements![0].GetOrNull("Future")?.GetString());
        Assert.AreEqual(Nwn.Authoring.Documents.NimGff.GffFieldType.ResRef, saved.Root.Get("Sounds").Elements![0].GetOrNull("Sound")?.Type);

        editor.SelectedEntry = editor.Rows[0];
        editor.MoveDownCommand.Execute(null);
        CollectionAssert.AreEqual(new[] { "wind", "as_cv_ta-da1", "rain" }, store.GetResRefList("Sounds", "Sound").ToArray());
        Assert.AreEqual("retained-invalid", document.Root.Get("Sounds").Elements![1].GetOrNull("Future")?.GetString());
        Assert.AreEqual(17u, document.Root.Get("Sounds").Elements![1].StructId);
        editor.SelectedEntry = editor.Rows[1];
        editor.RemoveCommand.Execute(null);
        CollectionAssert.AreEqual(new[] { "wind", "rain" }, store.GetResRefList("Sounds", "Sound").ToArray());
        Assert.AreEqual("retained-first", document.Root.Get("Sounds").Elements![0].GetOrNull("Future")?.GetString());
        session.Undo();
        CollectionAssert.AreEqual(new[] { "wind", "as_cv_ta-da1", "rain" }, store.GetResRefList("Sounds", "Sound").ToArray());
        var reopened = NativeRoundTrip(document);
        Assert.AreEqual("as_cv_ta-da1", reopened.Root.Get("Sounds").Elements![1].GetOrNull("Sound")!.GetString());
        Assert.AreEqual(17u, reopened.Root.Get("Sounds").Elements![1].StructId);
        Assert.AreEqual("retained-invalid", reopened.Root.Get("Sounds").Elements![1].GetOrNull("Future")?.GetString());
        Assert.AreEqual(Nwn.Authoring.Documents.NimGff.GffFieldType.ResRef, reopened.Root.Get("Sounds").Elements![1].GetOrNull("Sound")?.Type);
    }

    [TestMethod]
    public void SearchKeepsTheTwoHundredResultCapAndRefusedAddLeavesDocumentUnchanged()
    {
        var document = CreateDocument();
        var store = new BehaviorValueStore(document.Root);
        var before = document.ToBytes();
        var catalog = Enumerable.Range(0, 205).Select(index => $"sound{index:D3}").ToArray();
        var editor = new SoundListEditorViewModel(store, new("Sounds", "Sound"), catalog, 0,
            (_, _) => false, () => { });
        Assert.AreEqual(200, editor.FilteredSounds.Count);
        editor.Search = "sound204";
        CollectionAssert.AreEqual(new[] { "sound204" }, editor.FilteredSounds.ToArray());
        editor.Candidate = "unknown";
        Assert.IsFalse(editor.AddCommand.CanExecute(null));
        editor.Candidate = "sound204";
        editor.AddCommand.Execute(null);
        CollectionAssert.AreEqual(before, document.ToBytes());
    }

    private static JsonGffDocument CreateLegacyDocument() => JsonGffDocument.Parse(Encoding.UTF8.GetBytes("""
        {"__data_type":"UTS ","Sounds":{"type":"list","value":[
          {"__struct_id":17,"Sound":{"type":"resref","value":"as_cv_ta-da1"},"Future":{"type":"cexostring","value":"retained-invalid"}},
          {"__struct_id":23,"Sound":{"type":"resref","value":"wind"},"Future":{"type":"cexostring","value":"retained-first"}}
        ]}}
        """));

    private static JsonGffDocument NativeRoundTrip(JsonGffDocument document) =>
        NativeGffBridge.ToJsonDocument(GffReader.Read(GffWriter.Write(NativeGffBridge.ToNativeDocument(document))), true, true);

    private sealed class RecordingSoundListPreview : ISoundListPreview
    {
        public bool IsAvailable => true;
        public string? PlayedResRef { get; private set; }
        public string? Play(string? resRef)
        {
            PlayedResRef = resRef;
            return null;
        }
        public void Stop() { }
    }

    private static JsonGffDocument CreateDocument() => JsonGffDocument.Parse(Encoding.UTF8.GetBytes("""
        {"__data_type":"UTS ","Sounds":{"type":"list","value":[
          {"Sound":{"type":"resref","value":"wind"},"Future":{"type":"cexostring","value":"retained-first"}},
          {"Sound":{"type":"resref","value":"rain"},"Future":{"type":"cexostring","value":"retained-second"}}
        ]}}
        """));
}
