using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Behaviors;
using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Editing;

namespace Nwn.Authoring.Tests.Behaviors;

[TestClass]
public sealed class BehaviorValueStoreTests
{
    [TestMethod]
    public void FieldAndLocalEditsShareTransactionHistoryAndPreserveUnknownData()
    {
        var document = CreateDocument();
        var original = document.ToBytes();
        using var session = new DocumentSession("shared-store.utt.json", document);
        var store = new BehaviorValueStore(document.Root);
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            store.SetString(BehaviorFieldStorage.Field, "Tag", GffFieldType.CExoString, "outside"));
        session.Execute("Edit transition", () =>
        {
            store.SetString(BehaviorFieldStorage.Field, "LinkedTo", GffFieldType.CExoString, "destination");
            store.SetInteger(BehaviorFieldStorage.Field, "LinkedToFlags", GffFieldType.Byte, 2);
            store.SetString(BehaviorFieldStorage.Local, "MESSAGE", GffFieldType.CExoString, "authored text");
        });
        Assert.AreEqual(2L, store.GetInteger(BehaviorFieldStorage.Field, "LinkedToFlags"));
        Assert.AreEqual("retained", document.Root.GetStringOrNull("FutureField"));
        Assert.AreEqual("authored text", store.GetString(BehaviorFieldStorage.Local, "MESSAGE"));
        session.Undo();
        CollectionAssert.AreEqual(original, document.ToBytes());
        session.Redo();
        var reopened = new BehaviorValueStore(JsonGffDocument.Parse(document.ToBytes()).Root);
        Assert.AreEqual("destination", reopened.GetString(BehaviorFieldStorage.Field, "LinkedTo"));
        Assert.AreEqual("authored text", reopened.GetString(BehaviorFieldStorage.Local, "MESSAGE"));
    }

    [TestMethod]
    public void LocalizedCopyPreservesAllEntriesAndTlkReferenceWithoutSharingStorage()
    {
        var document = CreateDocument();
        var source = document.Root.GetOrAddLocString("LocalizedName");
        source.SetText("0", "English");
        source.SetText("1", "English female");
        source.SetText("2", "French");
        document.Root.Get("LocalizedName").SetLocStringId(1234);
        using var session = new DocumentSession("localized-store.utt.json", document);
        var store = new BehaviorValueStore(document.Root);
        session.Execute("Copy localized name", () => store.CopyLocalizedValue("LocalizedName", "Description"));
        Assert.IsTrue(store.LocalizedValuesMatch("LocalizedName", "Description"));
        Assert.AreEqual(1234u, store.GetLocalizedStringRef("Description"));
        session.Execute("Edit copied name", () => store.SetLocalizedText("Description", "Changed"));
        Assert.AreEqual("English", source.Text);
        Assert.AreEqual("English female", document.Root.GetLocStringOrNull("Description")!.GetText("1"));
        Assert.AreEqual("French", document.Root.GetLocStringOrNull("Description")!.GetText("2"));
        Assert.IsNull(store.GetLocalizedStringRef("Description"));
        Assert.AreEqual(1234u, store.GetLocalizedStringRef("LocalizedName"));
        session.Undo();
        Assert.IsTrue(store.LocalizedValuesMatch("LocalizedName", "Description"));
    }

    [TestMethod]
    public void ManagedValuesRespectPlacementScopeAndClearOnlyDeclaredOwnership()
    {
        var document = CreateDocument();
        using var session = new DocumentSession("managed-store.utt.json", document);
        var store = new BehaviorValueStore(document.Root);
        var template = new BehaviorManagedValue
        {
            Label = "Blueprint", Name = "TemplateResRef", FieldType = GffFieldType.ResRef,
            StringValue = "transition", IsInstanceOnly = true, ClearOnSwap = false,
        };
        var kind = new BehaviorManagedValue
        {
            Label = "Type", Name = "Type", FieldType = GffFieldType.Int, IntValue = 1,
        };
        var destination = new BehaviorFieldDefinition
        {
            Label = "Destination", Name = "LinkedTo", FieldType = GffFieldType.CExoString,
            Kind = BehaviorFieldKind.TagReference,
        };
        session.Execute("Apply blueprint", () => store.Apply(template, isInstance: false));
        Assert.AreEqual("fixture", store.GetString(BehaviorFieldStorage.Field, "TemplateResRef"));
        session.Execute("Apply placement", () =>
        {
            store.Apply(template);
            store.Apply(kind);
            store.SetString(BehaviorFieldStorage.Field, "LinkedTo", GffFieldType.CExoString, "destination");
            store.SetString(BehaviorFieldStorage.Local, "UNKNOWN_LOCAL", GffFieldType.CExoString, "retained local");
        });
        Assert.IsTrue(store.Matches(template));
        Assert.IsTrue(store.Matches(kind));
        session.Execute("Clear declared ownership", () => store.Clear([template, kind], [destination]));
        Assert.AreEqual("transition", store.GetString(BehaviorFieldStorage.Field, "TemplateResRef"));
        Assert.AreEqual(0L, store.GetInteger(BehaviorFieldStorage.Field, "Type"));
        Assert.AreEqual(string.Empty, store.GetString(BehaviorFieldStorage.Field, "LinkedTo"));
        Assert.AreEqual("retained local", store.GetString(BehaviorFieldStorage.Local, "UNKNOWN_LOCAL"));
        Assert.AreEqual("retained", document.Root.GetStringOrNull("FutureField"));
    }

    [TestMethod]
    public void InvalidNativeIntegersAndResrefsRollBackInsteadOfTruncating()
    {
        var document = CreateDocument();
        var original = document.ToBytes();
        using var session = new DocumentSession("range-store.utt.json", document);
        var store = new BehaviorValueStore(document.Root);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => session.Execute("Invalid byte", () =>
            store.SetInteger(BehaviorFieldStorage.Field, "LinkedToFlags", GffFieldType.Byte, 256)));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => session.Execute("Invalid unsigned", () =>
            store.SetInteger(BehaviorFieldStorage.Field, "LoadScreenID", GffFieldType.Word, -1)));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => session.Execute("Invalid local", () =>
            store.SetInteger(BehaviorFieldStorage.Local, "COUNT", GffFieldType.Dword64, (long)int.MaxValue + 1)));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => session.Execute("Invalid resource list", () =>
            store.AddResRefListEntry("Sounds", "Sound", "seventeen_chars_x")));
        CollectionAssert.AreEqual(original, document.ToBytes());
        session.Execute("Valid resource list", () => store.ReplaceResRefList("Sounds", "Sound", ["first", "second", "third"]));
        session.Execute("Reorder list", () => store.MoveListEntry("Sounds", 0, 2));
        CollectionAssert.AreEqual(new[] { "second", "third", "first" }, store.GetResRefList("Sounds", "Sound").ToArray());
        session.Undo();
        CollectionAssert.AreEqual(new[] { "first", "second", "third" }, store.GetResRefList("Sounds", "Sound").ToArray());
    }

    private static JsonGffDocument CreateDocument() => JsonGffDocument.Parse(Encoding.UTF8.GetBytes("""
        {"__data_type":"UTT ",
         "Tag":{"type":"cexostring","value":"fixture"},
         "TemplateResRef":{"type":"resref","value":"fixture"},
         "FutureField":{"type":"cexostring","value":"retained"}}
        """));
}
