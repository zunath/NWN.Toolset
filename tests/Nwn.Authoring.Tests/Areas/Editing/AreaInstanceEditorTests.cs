using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;
using Nwn.Authoring.Areas.Editing;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Editing;
using Nwn.Authoring.Resources;

namespace Nwn.Authoring.Tests.Areas.Editing;

[TestClass]
public sealed class AreaInstanceEditorTests
{
    [TestMethod]
    public void AddingAndDuplicatingKeepGitAndGicListsAlignedUnderUndo()
    {
        var instance = CreateInstance("source");
        using var documents = CreateDocuments();
        var editor = new AreaInstanceEditor(documents);

        Assert.IsTrue(editor.Add(ModuleResourceType.Utc, instance, "note"));
        Assert.AreEqual(1, documents.Instances.Document.Root.Get("Creature List").Elements!.Count);
        Assert.AreEqual(1, documents.Comments.Document.Root.Get("Creature List").Elements!.Count);
        Assert.AreEqual("note", documents.Comments.Document.Root.Get("Creature List").Elements![0]
            .Get("Comment").GetString());

        Assert.IsTrue(editor.Duplicate(ModuleResourceType.Utc, 0));
        Assert.AreEqual(2, documents.Instances.Document.Root.Get("Creature List").Elements!.Count);
        Assert.AreEqual(2, documents.Comments.Document.Root.Get("Creature List").Elements!.Count);
        Assert.AreEqual("note", documents.Comments.Document.Root.Get("Creature List").Elements![1]
            .Get("Comment").GetString());

        Assert.IsTrue(documents.UndoInstances());
        Assert.AreEqual(1, documents.Instances.Document.Root.Get("Creature List").Elements!.Count);
        Assert.AreEqual(1, documents.Comments.Document.Root.Get("Creature List").Elements!.Count);
    }

    [TestMethod]
    public void DeletingMultipleRowsHighestFirstPreservesRemainingPairing()
    {
        var first = CreateInstance("first");
        var second = CreateInstance("second");
        var third = CreateInstance("third");
        using var documents = CreateDocuments();
        var editor = new AreaInstanceEditor(documents);
        editor.Add(ModuleResourceType.Utc, first, "one");
        editor.Add(ModuleResourceType.Utc, second, "two");
        editor.Add(ModuleResourceType.Utc, third, "three");

        Assert.IsTrue(editor.Delete(ModuleResourceType.Utc, new[] { 0, 2, 2 }));
        var instances = documents.Instances.Document.Root.Get("Creature List").Elements!;
        var comments = documents.Comments.Document.Root.Get("Creature List").Elements!;
        Assert.AreEqual(1, instances.Count);
        Assert.AreEqual("second", instances[0].Get("Tag").GetString());
        Assert.AreEqual(1, comments.Count);
        Assert.AreEqual("two", comments[0].Get("Comment").GetString());
    }

    [TestMethod]
    public void TransformUsesSharedInstanceFieldContractAsSingleUndoStep()
    {
        var instance = CreateInstance("source");
        using var documents = CreateDocuments();
        var editor = new AreaInstanceEditor(documents);
        editor.Add(ModuleResourceType.Utc, instance);

        Assert.IsTrue(editor.SetTransform(ModuleResourceType.Utc, 0, 3f, 4f, 5f, 0f, 1f));
        var item = documents.Instances.Document.Root.Get("Creature List").Elements![0];
        Assert.AreEqual(3f, item.Get("XPosition").GetSingle());
        Assert.AreEqual(4f, item.Get("YPosition").GetSingle());
        Assert.AreEqual(5f, item.Get("ZPosition").GetSingle());

        Assert.IsTrue(documents.UndoInstances());
        item = documents.Instances.Document.Root.Get("Creature List").Elements![0];
        Assert.AreEqual(0f, item.Get("XPosition").GetSingle());
        Assert.AreEqual(0f, item.Get("YPosition").GetSingle());
        Assert.AreEqual(1f, item.Get("XOrientation").GetSingle());
    }

    private static AreaDocumentEditSession CreateDocuments()
    {
        var area = CreateDocument("ARE ", "Tag");
        var git = CreateDocument("GIT ", "Tag");
        var gic = CreateDocument("GIC ", "Tag");
        return new AreaDocumentEditSession(
            new DocumentSession("area.are", area),
            new DocumentSession("area.git", git),
            new DocumentSession("area.gic", gic));
    }

    private static JsonGffDocument CreateDocument(string type, string field)
    {
        var root = JsonGffField.CreateStruct(0).Struct!;
        root.Add(field, StringField(string.Empty));
        return new JsonGffDocument(type, root);
    }

    private static JsonGffStruct CreateInstance(string tag)
    {
        var instance = JsonGffField.CreateStruct(0).Struct!;
        instance.Add("Tag", StringField(tag));
        instance.Add("XPosition", FloatField(0f));
        instance.Add("YPosition", FloatField(0f));
        instance.Add("ZPosition", FloatField(0f));
        instance.Add("XOrientation", FloatField(1f));
        instance.Add("YOrientation", FloatField(0f));
        return instance;
    }

    private static JsonGffField StringField(string value) => JsonGffField.CreateScalar(
        GffFieldType.CExoString, Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(value)));

    private static JsonGffField FloatField(float value) => JsonGffField.CreateScalar(
        GffFieldType.Float, Encoding.ASCII.GetBytes(value.ToString("R", System.Globalization.CultureInfo.InvariantCulture)));
}
