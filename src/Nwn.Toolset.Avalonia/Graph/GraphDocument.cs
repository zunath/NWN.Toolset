using System.Collections.ObjectModel;

namespace Nwn.Toolset.Avalonia.Graph;

/// <summary>An immutable diagram, including unresolved targets in unfinished drafts.</summary>
public sealed class GraphDocument
{
    public const int MaximumNodes = 10_000;
    public const int MaximumEdges = 20_000;

    public GraphDocument(IEnumerable<GraphNode> nodes, IEnumerable<GraphEdge> edges)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(edges);
        var nodeArray = nodes.Take(MaximumNodes + 1).ToArray();
        var edgeArray = edges.Take(MaximumEdges + 1).ToArray();
        if (nodeArray.Length > MaximumNodes || edgeArray.Length > MaximumEdges)
            throw new ArgumentOutOfRangeException(nameof(nodes), "The diagram exceeds its node or edge budget.");
        var indexedNodes = new Dictionary<GraphNodeId, GraphNode>();
        foreach (var node in nodeArray)
        {
            ArgumentNullException.ThrowIfNull(node);
            ValidateId(node.Id.Value);
            ValidateText(node.Title);
            ValidateText(node.Subtitle);
            if (!indexedNodes.TryAdd(node.Id, node)) throw new ArgumentException("Node identities must be unique.", nameof(nodes));
        }
        var edgeIds = new HashSet<GraphEdgeId>();
        foreach (var edge in edgeArray)
        {
            ArgumentNullException.ThrowIfNull(edge);
            ValidateId(edge.Id.Value);
            ValidateId(edge.Source.Value);
            ValidateId(edge.Target.Value);
            ValidateText(edge.Label);
            if (!edgeIds.Add(edge.Id)) throw new ArgumentException("Edge identities must be unique.", nameof(edges));
            if (!indexedNodes.ContainsKey(edge.Source)) throw new ArgumentException("An edge must have a visible source node.", nameof(edges));
        }
        Nodes = Array.AsReadOnly(nodeArray);
        Edges = Array.AsReadOnly(edgeArray);
        NodesById = new ReadOnlyDictionary<GraphNodeId, GraphNode>(indexedNodes);
    }

    public IReadOnlyList<GraphNode> Nodes { get; }
    public IReadOnlyList<GraphEdge> Edges { get; }
    public IReadOnlyDictionary<GraphNodeId, GraphNode> NodesById { get; }

    private static void ValidateId(string value) => ArgumentException.ThrowIfNullOrWhiteSpace(value);
    private static void ValidateText(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length > 2048) throw new ArgumentOutOfRangeException(nameof(value), "Diagram labels are bounded to 2,048 characters.");
    }
}
