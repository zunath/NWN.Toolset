using System.Text.Json;
using System.Text.Json.Nodes;

namespace Nwn.Authoring.Documents.Json;

/// <summary>Reads and writes generic JSON DOM documents with deterministic compact formatting.</summary>
public sealed class JsonDomCodec : IDocumentCodec<JsonDomDocument>
{
    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = false };

    public JsonDomDocument Decode(ReadOnlyMemory<byte> bytes) =>
        new(JsonNode.Parse(bytes.Span));

    public byte[] Encode(JsonDomDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return JsonSerializer.SerializeToUtf8Bytes(document.Root, WriteOptions);
    }
}
