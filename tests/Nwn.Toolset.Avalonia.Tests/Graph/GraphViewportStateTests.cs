using Avalonia;
using Nwn.Toolset.Avalonia.Graph;

namespace Nwn.Toolset.Avalonia.Tests.Graph;

[TestClass]
public sealed class GraphViewportStateTests
{
    [TestMethod]
    public void CyclesDisconnectedComponentsAndUnresolvedTargetsHaveDeterministicNonoverlappingLayout()
    {
        var document = Document();
        var first = new GraphViewportState();
        var second = new GraphViewportState();
        first.SetDocument(document);
        second.SetDocument(document);
        Assert.AreEqual(5, first.NodePositions.Count);
        foreach (var pair in first.NodePositions)
        {
            Assert.AreEqual(pair.Value, second.NodePositions[pair.Key]);
            var bounds = new Rect(pair.Value, new Size(GraphViewportState.NodeWidth, GraphViewportState.NodeHeight));
            foreach (var other in first.NodePositions.Where(other => other.Key != pair.Key))
                Assert.IsFalse(bounds.Intersects(new Rect(other.Value, bounds.Size)));
        }
    }

    [TestMethod]
    public void SelectionHitTestingAndDraggingSurvivePanZoomAndDocumentRefresh()
    {
        var state = new GraphViewportState();
        state.SetDocument(Document());
        var selected = new GraphNodeId("branch-a");
        state.Select(selected);
        state.MoveNode(selected, new Point(730, 390));
        var center = new Point(820, 425);
        state.Pan(new Vector(-40, 30));
        state.ZoomAt(2, new Point(330, 220));
        Assert.AreEqual(selected, state.HitTest(state.ToScreen(center)));
        state.SetDocument(Document());
        Assert.AreEqual(selected, state.SelectedNode);
        Assert.AreEqual(new Point(730, 390), state.NodePositions[selected]);
        state.SetDocument(new GraphDocument([new(new GraphNodeId("replacement"), "New graph")], []));
        Assert.IsNull(state.SelectedNode);
        Assert.IsFalse(state.NodePositions.ContainsKey(selected));
    }

    [TestMethod]
    public void ZoomKeepsThePointerAnchorAndFitShowsAllOrdinaryNodes()
    {
        var state = new GraphViewportState();
        state.SetDocument(Document());
        var pointer = new Point(390, 270);
        var anchor = state.ToGraph(pointer);
        state.ZoomAt(3, pointer);
        Assert.AreEqual(anchor.X, state.ToGraph(pointer).X, 0.000001);
        Assert.AreEqual(anchor.Y, state.ToGraph(pointer).Y, 0.000001);
        state.Fit(new Size(1000, 700));
        foreach (var position in state.NodePositions.Values)
        {
            var topLeft = state.ToScreen(position);
            var bottomRight = state.ToScreen(position + new Vector(GraphViewportState.NodeWidth, GraphViewportState.NodeHeight));
            Assert.IsTrue(topLeft.X >= 0 && topLeft.Y >= 0 && bottomRight.X <= 1000 && bottomRight.Y <= 700);
        }
        state.ZoomAt(double.MaxValue, pointer);
        Assert.AreEqual(2, state.Scale);
        state.ZoomAt(-double.MaxValue, pointer);
        Assert.AreEqual(0.1, state.Scale);
        Assert.IsTrue(double.IsFinite(state.Offset.X) && double.IsFinite(state.Offset.Y));
    }

    [TestMethod]
    public void InvalidInputCannotPoisonSelectionOrCoordinates()
    {
        var state = new GraphViewportState();
        state.SetDocument(Document());
        Assert.ThrowsExactly<ArgumentException>(() => state.Select(new GraphNodeId("absent")));
        Assert.ThrowsExactly<ArgumentException>(() => state.MoveNode(new GraphNodeId("absent"), default));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => state.MoveNode(new GraphNodeId("entry"), new Point(double.NaN, 0)));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => state.Pan(new Vector(double.PositiveInfinity, 0)));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => state.ZoomAt(double.NaN, default));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => state.Fit(new Size(0, 700)));
        state.SetDocument(new GraphDocument([], []));
        state.Fit(new Size(500, 300));
        Assert.IsNull(state.HitTest(default));
    }

    private static GraphDocument Document() => new(
        [new(new("entry"), "Greeting"), new(new("branch-a"), "Help"), new(new("branch-b"), "Refuse"),
            new(new("cycle-a"), "Loop A"), new(new("cycle-b"), "Loop B")],
        [new(new("accept"), new("entry"), new("branch-a")), new(new("refuse"), new("entry"), new("branch-b")),
            new(new("back"), new("branch-a"), new("entry")), new(new("cycle1"), new("cycle-a"), new("cycle-b")),
            new(new("cycle2"), new("cycle-b"), new("cycle-a")), new(new("draft"), new("branch-b"), new("unfinished"))]);
}
