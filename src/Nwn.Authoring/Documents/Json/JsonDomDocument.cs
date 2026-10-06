using System.Text.Json.Nodes;

namespace Nwn.Authoring.Documents.Json;

/// <summary>A generic JSON document DOM that retains properties, values, and unknown fields.</summary>
public sealed class JsonDomDocument(JsonNode? root)
{
    public JsonNode? Root { get; set; } = root;
}
