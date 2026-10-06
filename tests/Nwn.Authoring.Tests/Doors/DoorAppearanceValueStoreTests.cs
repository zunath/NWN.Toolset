using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Behaviors;
using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Doors;
using Nwn.Authoring.Editing;
using Nwn.Formats.Gff;
using EditableGffFieldType = Nwn.Authoring.Documents.NimGff.GffFieldType;

namespace Nwn.Authoring.Tests.Doors;

[TestClass]
public sealed class DoorAppearanceValueStoreTests
{
    [TestMethod]
    public void ReadUsesSpecificThenNewGenericThenLegacyWithoutMutatingUnresolvedData()
    {
        var empty = CreateDocument(string.Empty);
        var emptyBytes = empty.ToBytes();
        Assert.AreEqual(new DoorAppearanceSelection(DoorAppearanceKind.Generic, 0),
            DoorAppearanceValueStore.Read(new BehaviorValueStore(empty.Root)));
        CollectionAssert.AreEqual(emptyBytes, empty.ToBytes());

        var legacy = CreateDocument("""
            "GenericType":{"type":"dword","value":12},
            "FutureField":{"type":"cexostring","value":"retained"}
            """);
        var legacyBytes = legacy.ToBytes();
        Assert.AreEqual(new DoorAppearanceSelection(DoorAppearanceKind.Generic, 12),
            DoorAppearanceValueStore.Read(new BehaviorValueStore(legacy.Root)));
        CollectionAssert.AreEqual(legacyBytes, legacy.ToBytes());

        var newZero = CreateDocument("""
            "GenericType":{"type":"dword","value":12},
            "GenericType_New":{"type":"dword","value":0}
            """);
        Assert.AreEqual(new DoorAppearanceSelection(DoorAppearanceKind.Generic, 0),
            DoorAppearanceValueStore.Read(new BehaviorValueStore(newZero.Root)));

        var large = CreateDocument("""
            "GenericType_New":{"type":"dword","value":4294967295}
            """);
        Assert.AreEqual(new DoorAppearanceSelection(DoorAppearanceKind.Generic, uint.MaxValue),
            DoorAppearanceValueStore.Read(new BehaviorValueStore(large.Root)));

        var specific = CreateDocument("""
            "Appearance":{"type":"dword","value":987654},
            "GenericType_New":{"type":"dword","value":12}
            """);
        Assert.AreEqual(new DoorAppearanceSelection(DoorAppearanceKind.Specific, 987654),
            DoorAppearanceValueStore.Read(new BehaviorValueStore(specific.Root)));
        Assert.AreEqual("retained", legacy.Root.GetStringOrNull("FutureField"));
    }

    [TestMethod]
    public void WriteChangesOnlyPairedFieldsAndRoundTripsBothVariantsThroughNativeGff()
    {
        var document = CreateDocument("""
            "Appearance":{"type":"dword","value":23},
            "GenericType":{"type":"dword","value":7},
            "GenericType_New":{"type":"dword","value":4},
            "FutureField":{"type":"cexostring","value":"retained"}
            """);
        using var session = new DocumentSession("door-appearance.utd.json", document);
        var store = new BehaviorValueStore(document.Root);

        session.Execute("Set generic appearance", () =>
            DoorAppearanceValueStore.Write(store, new DoorAppearanceSelection(DoorAppearanceKind.Generic, 42)));
        AssertNativeRoundTrip(document, new DoorAppearanceSelection(DoorAppearanceKind.Generic, 42));

        session.Execute("Set specific appearance", () =>
            DoorAppearanceValueStore.Write(store, new DoorAppearanceSelection(DoorAppearanceKind.Specific, 31)));
        AssertNativeRoundTrip(document, new DoorAppearanceSelection(DoorAppearanceKind.Specific, 31));
        Assert.AreEqual(7L, store.GetInteger(BehaviorFieldStorage.Field, "GenericType"));
        Assert.AreEqual("retained", document.Root.GetStringOrNull("FutureField"));

        session.Undo();
        Assert.AreEqual(new DoorAppearanceSelection(DoorAppearanceKind.Generic, 42),
            DoorAppearanceValueStore.Read(store));
        session.Undo();
        Assert.AreEqual(23L, store.GetInteger(BehaviorFieldStorage.Field, "Appearance"));
        Assert.AreEqual(4L, store.GetInteger(BehaviorFieldStorage.Field, "GenericType_New"));
        session.Redo();
        Assert.AreEqual(new DoorAppearanceSelection(DoorAppearanceKind.Generic, 42),
            DoorAppearanceValueStore.Read(store));
        session.Redo();
        Assert.AreEqual(new DoorAppearanceSelection(DoorAppearanceKind.Specific, 31),
            DoorAppearanceValueStore.Read(store));
    }

    [TestMethod]
    public void WriteRejectsOutOfRangeDwordBeforeChangingEitherField()
    {
        var document = CreateDocument("""
            "Appearance":{"type":"dword","value":3},
            "GenericType_New":{"type":"dword","value":9}
            """);
        var original = document.ToBytes();
        using var session = new DocumentSession("door-appearance-range.utd.json", document);
        var store = new BehaviorValueStore(document.Root);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => session.Execute("Set oversized appearance", () =>
            DoorAppearanceValueStore.Write(
                store, new DoorAppearanceSelection(DoorAppearanceKind.Generic, (long)uint.MaxValue + 1))));
        CollectionAssert.AreEqual(original, document.ToBytes());
    }

    private static void AssertNativeRoundTrip(JsonGffDocument document, DoorAppearanceSelection expected)
    {
        var editable = JsonGffDocument.Parse(document.ToBytes());
        var native = NativeGffBridge.ToNativeDocument(editable);
        var bytes = GffWriter.Write(native);
        var reopened = NativeGffBridge.ToJsonDocument(
            GffReader.Read(bytes), encodeTextAsUtf8: true, preserveNativeFieldOrder: true);
        var store = new BehaviorValueStore(reopened.Root);

        Assert.AreEqual(expected, DoorAppearanceValueStore.Read(store));
        Assert.AreEqual(EditableGffFieldType.Dword, reopened.Root.Get("Appearance").Type);
        Assert.AreEqual(EditableGffFieldType.Dword, reopened.Root.Get("GenericType_New").Type);
        Assert.AreEqual(EditableGffFieldType.Dword, reopened.Root.Get("GenericType").Type);
        Assert.AreEqual("retained", reopened.Root.GetStringOrNull("FutureField"));
    }

    private static JsonGffDocument CreateDocument(string fields)
    {
        var separator = string.IsNullOrEmpty(fields) ? string.Empty : ",";
        var json = @"{""__data_type"":""UTD """ + separator + fields + "}";
        return JsonGffDocument.Parse(Encoding.UTF8.GetBytes(json));
    }
}
