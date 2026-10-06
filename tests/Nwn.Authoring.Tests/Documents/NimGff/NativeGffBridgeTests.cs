using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Editing;
using Nwn.Formats.Gff;
using NativeField = Nwn.Formats.Gff.GffField;

namespace Nwn.Authoring.Tests.Documents.NimGff;

[TestClass]
public sealed class NativeGffBridgeTests
{
    [TestMethod]
    public void BridgePreservesEveryNativeFieldTypeAndStructureIds()
    {
        var source = new GffDocument
        {
            FileType = "ARE ",
            Root = new GffStruct(91)
                .Add(NativeField.Byte("Byte", 200))
                .Add(NativeField.Char("Char", -4))
                .Add(NativeField.Word("Word", 6000))
                .Add(NativeField.Short("Short", -1234))
                .Add(NativeField.Dword("Dword", 4000000000))
                .Add(NativeField.Int("Int", -70000))
                .Add(NativeField.Dword64("Dword64", 18000000000000000000UL))
                .Add(NativeField.Int64("Int64", -9000000000000000000L))
                .Add(NativeField.Float("Float", 1.125f))
                .Add(NativeField.Double("Double", -3.25))
                .Add(NativeField.String("Text", "Łódź — Café"))
                .Add(NativeField.ResRef("Resource", "tic01"))
                .Add(NativeField.LocString("Localized", new GffLocString { StringRef = 42, Strings = [new(19, "Other locale"), new(0, "English")] }))
                .Add(NativeField.Void("Bytes", [0, 1, 255, 32, 34]))
                .Add(NativeField.Struct("Struct", new GffStruct(4).Add(NativeField.Int("Value", 7))))
                .Add(NativeField.List("List", [new GffStruct(8).Add(NativeField.Float("X", 0.25f))]))
        };
        var editable = NativeGffBridge.ToJsonDocument(source, encodeTextAsUtf8: true, preserveNativeFieldOrder: true);
        var roundTrip = NativeGffBridge.ToNativeDocument(JsonGffDocument.Parse(editable.ToBytes()));
        Assert.AreEqual(source.Root.StructId, roundTrip.Root.StructId);
        CollectionAssert.AreEqual(GffWriter.Write(source), GffWriter.Write(roundTrip));
    }

    [TestMethod]
    public void NativeOrderBridgeRetainsExactFloatAndDoubleBits()
    {
        var source = new GffDocument
        {
            FileType = "ARE ",
            Root = new GffStruct(91)
                .Add(NativeField.Float("Float", 12.345678f))
                .Add(NativeField.Double("Double", 0.12345678901234566))
        };

        var editable = NativeGffBridge.ToJsonDocument(
            source,
            encodeTextAsUtf8: false,
            preserveNativeFieldOrder: true);
        var roundTrip = NativeGffBridge.ToNativeDocument(JsonGffDocument.Parse(editable.ToBytes()));

        CollectionAssert.AreEqual(GffWriter.Write(source), GffWriter.Write(roundTrip));
    }

    [TestMethod]
    public void Utf8HostPreferenceAppliesToEditsOfAsciiFieldsAndNewLocalizedEntries()
    {
        var source = new GffDocument
        {
            FileType = "ARE ",
            Root = new GffStruct()
                .Add(NativeField.String("Name", "ASCII"))
                .Add(NativeField.LocString("Localized", new GffLocString
                {
                    Strings = [new GffLocStringEntry(0, "ASCII")]
                }))
        };
        var editable = NativeGffBridge.ToJsonDocument(
            source,
            encodeTextAsUtf8: true,
            preserveNativeFieldOrder: true);
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".are.json");
        using var session = new DocumentSession(path, editable);
        var name = editable.Root.Get("Name");
        var localized = editable.Root.Get("Localized");
        Assert.IsTrue(name.PreferUtf8Text);
        Assert.IsTrue(localized.PreferUtf8Text);

        session.Execute("Change text", () =>
        {
            name.SetString("漢字");
            localized.LocStringEntries![0].SetText("漢字");
            localized.AddLocStringEntry(new LocStringEntry("1", JsonStringCodec.Encode("old")));
            localized.LocStringEntries[1].SetText("新しい");
        });

        CollectionAssert.AreEqual(
            JsonStringCodec.Encode("漢字", useUtf8: true),
            name.RawValue!);
        CollectionAssert.AreEqual(
            JsonStringCodec.Encode("漢字", useUtf8: true),
            localized.LocStringEntries![0].RawText);
        CollectionAssert.AreEqual(
            JsonStringCodec.Encode("新しい", useUtf8: true),
            localized.LocStringEntries[1].RawText);
    }

    [TestMethod]
    public void TlkReferenceEditIsGuardedAndUndoRestoresExactTokens()
    {
        var source = new GffDocument { FileType = "ARE ", Root = new GffStruct().Add(NativeField.LocString("Name", GffLocString.FromStringRef(42))) };
        var editable = NativeGffBridge.ToJsonDocument(source);
        var original = editable.ToBytes();
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".are.json");
        using var session = new DocumentSession(path, editable);
        Assert.Throws<InvalidOperationException>(() => editable.Root.Get("Name").SetLocStringId(51));
        session.Execute("Change string reference", () => editable.Root.Get("Name").SetLocStringId(51));
        Assert.AreEqual(51u, new AreDocument(editable).Name.StrRef);
        session.UndoStack.Undo();
        CollectionAssert.AreEqual(original, editable.ToBytes());
        session.UndoStack.Redo();
        Assert.AreEqual(51u, NativeGffBridge.ToNativeDocument(editable).Root.Find("Name")!.AsLocString().StringRef);
    }

    [TestMethod]
    public void EmptyInlineTlkStringRemainsAReference()
    {
        var source = new GffDocument { FileType = "ARE ", Root = new GffStruct().Add(NativeField.LocString("Name", GffLocString.FromStringRef(42))) };
        var decoded = JsonGffDocument.Parse(NativeGffBridge.ToJsonDocument(source).ToBytes());
        var native = NativeGffBridge.ToNativeDocument(decoded).Root.Find("Name")!.AsLocString();
        Assert.AreEqual(42u, native.StringRef);
        Assert.AreEqual(0, native.Strings.Count);
    }
}
