using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Documents;
using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Editing;
using Nwn.Formats.Gff;
using System.Security.Cryptography;

namespace Nwn.Authoring.Tests.Editing;

[TestClass]
public sealed class DocumentSessionCodecTests
{
    [TestMethod]
    public void ReloadCopiesCodecOwnedSourceContextToTheExistingDocument()
    {
        var path = Path.Combine(Path.GetTempPath(), "native-session-context-" + Guid.NewGuid().ToString("N") + ".are");
        var codec = new BinaryAreaCodec();
        try
        {
            File.WriteAllBytes(path, GffWriter.Write(new GffDocument
            {
                FileType = "ARE ",
                Root = new GffStruct().Add(GffField.String("Tag", "before")),
            }));
            using var session = DocumentSession.Open(path, codec);
            var existingDocument = session.Document;
            var area = new AreDocument(session.Document);
            session.Execute("Edit", () => area.Tag = "after");
            File.WriteAllBytes(path, session.ToBytes());
            var expectedContext = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

            session.ReloadFromDisk();

            Assert.AreSame(existingDocument, session.Document);
            Assert.AreEqual(expectedContext, session.Document.SourceContext as string);
        }
        finally { File.Delete(path); }
    }

    [TestMethod]
    public void NativeHostCodecSurvivesTransactionsSnapshotsAndDiskReload()
    {
        var path = Path.Combine(Path.GetTempPath(), "native-session-" + Guid.NewGuid().ToString("N") + ".are");
        var codec = new BinaryAreaCodec();
        try
        {
            File.WriteAllBytes(path, GffWriter.Write(new GffDocument
            {
                FileType = "ARE ",
                Root = new GffStruct().Add(GffField.String("Tag", "original"))
                    .Add(GffField.String("FutureField", "retained")),
            }));
            using var session = DocumentSession.Open(path, codec);
            var area = new AreDocument(session.Document);
            session.Execute("Set tag", () => area.Tag = "changed");
            Assert.AreEqual("changed", GffReader.Read(session.ToBytes()).Root.Find("Tag")!.AsString());
            CollectionAssert.AreEqual(session.ToBytes(), DocumentSession.CaptureSnapshots(session)[0]);
            session.Undo();
            Assert.AreEqual("original", area.Tag);
            session.Redo();
            File.WriteAllBytes(path, session.ToBytes());
            session.ReloadFromDisk();
            Assert.AreEqual("changed", area.Tag);
            Assert.AreEqual("retained", GffReader.Read(session.ToBytes()).Root.Find("FutureField")!.AsString());
            Assert.IsFalse(session.HasExternalChange());
        }
        finally { File.Delete(path); }
    }

    private sealed class BinaryAreaCodec : IDocumentCodec<JsonGffDocument>
    {
        public JsonGffDocument Decode(ReadOnlyMemory<byte> bytes)
        {
            var document = NativeGffBridge.ToJsonDocument(GffReader.Read(bytes.ToArray()), true, true);
            document.SourceContext = Convert.ToHexString(SHA256.HashData(bytes.Span));
            return document;
        }

        public byte[] Encode(JsonGffDocument document) => GffWriter.Write(NativeGffBridge.ToNativeDocument(document));
    }
}
