using Nwn.Toolset.Avalonia.Graph;

namespace Nwn.Toolset.Avalonia.Tests.Graph;

[TestClass]
public sealed class GraphDocumentTests
{
    [TestMethod]
    public void DraftLinksRemainVisibleAndInputCollectionsCannotMutateTheDiagram()
    {
        var id = new GraphNodeId("entry");
        var nodes = new List<GraphNode> { new(id, "Greeting") };
        var edges = new List<GraphEdge> { new(new GraphEdgeId("choice"), id, new GraphNodeId("unfinished"), "Continue") };
        var document = new GraphDocument(nodes, edges);
        nodes.Clear();
        edges.Clear();
        Assert.AreEqual(1, document.Nodes.Count);
        Assert.AreEqual("unfinished", document.Edges.Single().Target.Value);
        Assert.IsFalse(document.NodesById.ContainsKey(document.Edges.Single().Target));
        Assert.ThrowsExactly<NotSupportedException>(() => ((IList<GraphNode>)document.Nodes).Clear());
    }

    [TestMethod]
    public void DuplicateIdentitiesAbsentSourcesAndOversizedDiagramsAreRejected()
    {
        var id = new GraphNodeId("entry");
        var node = new GraphNode(id, "Greeting");
        var edge = new GraphEdge(new GraphEdgeId("choice"), id, id);
        Assert.ThrowsExactly<ArgumentException>(() => new GraphDocument([node, node], []));
        Assert.ThrowsExactly<ArgumentException>(() => new GraphDocument([node], [edge, edge]));
        Assert.ThrowsExactly<ArgumentException>(() => new GraphDocument([], [edge]));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new GraphDocument(Enumerable.Repeat(node, GraphDocument.MaximumNodes + 1), []));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new GraphDocument([node], Enumerable.Repeat(edge, GraphDocument.MaximumEdges + 1)));
        Assert.ThrowsExactly<ArgumentNullException>(() => new GraphDocument([new(default, "Missing identity")], []));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new GraphDocument([new(id, new string('x', 2049))], []));
    }
}
