using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Behaviors;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Placeables;
using Nwn.Authoring.Editing;
using Nwn.Formats.Gff;
using EditableGffFieldType = Nwn.Authoring.Documents.NimGff.GffFieldType;

namespace Nwn.Authoring.Tests.Placeables;

[TestClass]
public sealed class PlaceableAppearanceValueStoreTests
{
    [TestMethod]
    public void ReadDoesNotMutateMissingOrUnknownFields()
    {
        var document = CreateDocument("\"FutureField\":{\"type\":\"cexostring\",\"value\":\"retained\"}");
        var original = document.ToBytes();
        var store = new BehaviorValueStore(document.Root);

        Assert.AreEqual(0L, PlaceableAppearanceValueStore.Read(store));
        CollectionAssert.AreEqual(original, document.ToBytes());
    }

    [TestMethod]
    public void WritePreservesExistingIntegerTypeAndUnknownFieldsAcrossNativeRoundTripAndUndo()
    {
        var document = CreateDocument("\"Appearance\":{\"type\":\"int\",\"value\":5},\"FutureField\":{\"type\":\"cexostring\",\"value\":\"retained\"}");
        using var session = new DocumentSession("placeable-appearance.utp.json", document);
        var store = new BehaviorValueStore(document.Root);

        session.Execute("Change placeable appearance", () => PlaceableAppearanceValueStore.Write(store, 17));

        Assert.AreEqual(17L, PlaceableAppearanceValueStore.Read(store));
        Assert.AreEqual(EditableGffFieldType.Int, document.Root.Get("Appearance").Type);
        AssertNativeRoundTrip(document, 17L);
        Assert.AreEqual("retained", document.Root.GetStringOrNull("FutureField"));

        session.Undo();
        Assert.AreEqual(5L, PlaceableAppearanceValueStore.Read(store));
        Assert.AreEqual(EditableGffFieldType.Int, document.Root.Get("Appearance").Type);
        session.Redo();
        Assert.AreEqual(17L, PlaceableAppearanceValueStore.Read(store));
    }

    [TestMethod]
    [DataRow("byte", EditableGffFieldType.Byte)]
    [DataRow("char", EditableGffFieldType.Char)]
    [DataRow("word", EditableGffFieldType.Word)]
    [DataRow("short", EditableGffFieldType.Short)]
    [DataRow("dword", EditableGffFieldType.Dword)]
    [DataRow("int", EditableGffFieldType.Int)]
    [DataRow("dword64", EditableGffFieldType.Dword64)]
    [DataRow("int64", EditableGffFieldType.Int64)]
    public void WritePreservesEverySupportedExistingIntegerType(string typeName, EditableGffFieldType expectedType)
    {
        var document = CreateDocument($"\"Appearance\":{{\"type\":\"{typeName}\",\"value\":5}}");
        var store = new BehaviorValueStore(document.Root);

        PlaceableAppearanceValueStore.Write(store, 17);

        Assert.AreEqual(17L, PlaceableAppearanceValueStore.Read(store));
        Assert.AreEqual(expectedType, document.Root.Get("Appearance").Type);
    }

    [TestMethod]
    public void ReadPreservesUnsignedAndSignedNativeValuesWithoutChangingTheDocument()
    {
        var unsigned = CreateDocument("\"Appearance\":{\"type\":\"dword\",\"value\":4294967295}");
        var signed = CreateDocument("\"Appearance\":{\"type\":\"int\",\"value\":-7}");
        var unsignedBytes = unsigned.ToBytes();
        var signedBytes = signed.ToBytes();

        Assert.AreEqual((long)uint.MaxValue, PlaceableAppearanceValueStore.Read(new BehaviorValueStore(unsigned.Root)));
        Assert.AreEqual(-7L, PlaceableAppearanceValueStore.Read(new BehaviorValueStore(signed.Root)));
        CollectionAssert.AreEqual(unsignedBytes, unsigned.ToBytes());
        CollectionAssert.AreEqual(signedBytes, signed.ToBytes());
    }

    [TestMethod]
    public void WriteCreatesUnsignedDwordWhenAppearanceIsAbsent()
    {
        var document = CreateDocument("\"FutureField\":{\"type\":\"cexostring\",\"value\":\"retained\"}");
        var store = new BehaviorValueStore(document.Root);

        PlaceableAppearanceValueStore.Write(store, 65535);

        Assert.AreEqual(65535L, PlaceableAppearanceValueStore.Read(store));
        Assert.AreEqual(EditableGffFieldType.Dword, document.Root.Get("Appearance").Type);
        AssertNativeRoundTrip(document, 65535L);
    }

    private static void AssertNativeRoundTrip(JsonGffDocument document, long expected)
    {
        var editable = JsonGffDocument.Parse(document.ToBytes());
        var native = NativeGffBridge.ToNativeDocument(editable);
        var bytes = GffWriter.Write(native);
        var reopened = NativeGffBridge.ToJsonDocument(GffReader.Read(bytes), encodeTextAsUtf8: true,
            preserveNativeFieldOrder: true);
        var store = new BehaviorValueStore(reopened.Root);

        Assert.AreEqual(expected, PlaceableAppearanceValueStore.Read(store));
        Assert.AreEqual("retained", reopened.Root.GetStringOrNull("FutureField"));
    }

    private static JsonGffDocument CreateDocument(string fields)
    {
        var separator = string.IsNullOrEmpty(fields) ? string.Empty : ",";
        var json = @"{""__data_type"":""UTP """ + separator + fields + "}";
        return JsonGffDocument.Parse(Encoding.UTF8.GetBytes(json));
    }
}
