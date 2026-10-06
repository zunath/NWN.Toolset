using Nwn.Formats.Gff;

namespace Nwn.Authoring.Documents.Gff;

/// <summary>Adapts the shared binary GFF reader and writer to document-session history.</summary>
public sealed class GffDocumentCodec : IDocumentCodec<GffDocument>
{
    public GffDocument Decode(ReadOnlyMemory<byte> bytes) => GffReader.Read(bytes.ToArray());

    public byte[] Encode(GffDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return GffWriter.Write(document);
    }
}
