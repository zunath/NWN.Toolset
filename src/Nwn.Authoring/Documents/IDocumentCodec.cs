namespace Nwn.Authoring.Documents;

/// <summary>Encodes and decodes a document value. Encoding also defines semantic equality and
/// detached copies for session history.</summary>
public interface IDocumentCodec<TDocument> where TDocument : class
{
    TDocument Decode(ReadOnlyMemory<byte> bytes);
    byte[] Encode(TDocument document);
}
