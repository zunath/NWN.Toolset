namespace Nwn.Authoring.Documents.NimGff;

/// <summary>Preserves the native nwn_gff JSON document representation.</summary>
public sealed class NimGffDocumentCodec : IDocumentCodec<JsonGffDocument>
{
    public JsonGffDocument Decode(ReadOnlyMemory<byte> bytes) => JsonGffDocument.Parse(bytes.ToArray());
    public byte[] Encode(JsonGffDocument document) => document.ToBytes();
}
