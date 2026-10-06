using Avalonia;
using Avalonia.Headless;
using Avalonia.Input;
using Nwn.Toolset.Avalonia.Graph;
using Nwn.Toolset.Avalonia.Tests.Support;

namespace Nwn.Toolset.Avalonia.Tests.Graph;

[TestClass]
[TestCategory("DesktopRender")]
public sealed class GraphCanvasTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task PointerSelectionDragAndAnchoredZoomWorkInTheRenderedControlAsync()
    {
        var selectedEvents = new List<GraphNodeId?>();
        await GraphTestRuntime.RunAsync(() =>
        {
            var graph = new GraphCanvas();
            graph.SetDocument(Diagram());
            graph.NodeSelected += (_, args) => selectedEvents.Add(args.NodeId);
            return graph;
        }, window =>
        {
            var graph = (GraphCanvas)window.Content!;
            graph.Fit();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            using var frame = window.CaptureRenderedFrame() ?? throw new AssertFailedException("The graph must produce a software-rendered frame.");
            var path = Path.Combine(TestContext.TestResultsDirectory!, "shared-directed-graph.png");
            frame.Save(path);
            TestContext.AddResultFile(path);
            var id = new GraphNodeId("entry");
            var node = graph.Viewport.NodePositions[id];
            var start = graph.Viewport.ToScreen(node + new Vector(90, 35));
            var scale = graph.Viewport.Scale;
            window.MouseDown(start, MouseButton.Left);
            window.MouseMove(start + new Vector(60, 35));
            window.MouseUp(start + new Vector(60, 35), MouseButton.Left);
            Assert.AreEqual(id, graph.Viewport.SelectedNode);
            CollectionAssert.AreEqual(new GraphNodeId?[] { id }, selectedEvents);
            Assert.AreEqual(node.X + 60 / scale, graph.Viewport.NodePositions[id].X, 0.001);
            Assert.AreEqual(node.Y + 35 / scale, graph.Viewport.NodePositions[id].Y, 0.001);
            var anchor = new Point(500, 400);
            var graphAnchor = graph.Viewport.ToGraph(anchor);
            window.MouseWheel(anchor, new Vector(0, -2));
            Assert.AreEqual(graphAnchor.X, graph.Viewport.ToGraph(anchor).X, 0.001);
            Assert.AreEqual(graphAnchor.Y, graph.Viewport.ToGraph(anchor).Y, 0.001);
            var beforePan = graph.Viewport.Offset;
            window.MouseDown(new Point(20, 20), MouseButton.Right);
            window.MouseMove(new Point(35, 45));
            window.MouseUp(new Point(35, 45), MouseButton.Right);
            Assert.AreEqual(beforePan + new Vector(15, 25), graph.Viewport.Offset);
        });
    }

    private static GraphDocument Diagram() => new(
        [new(new("entry"), "Gate attendant", "Offer two routes"), new(new("combat"), "Clear the route", "Combat objective"),
            new(new("interaction"), "Repair the relay", "Interaction objective"), new(new("return"), "Return to the gate", "Authored outcome")],
        [new(new("accept-a"), new("entry"), new("combat"), "Volunteer"), new(new("accept-b"), new("entry"), new("interaction"), "Repair"),
            new(new("a-return"), new("combat"), new("return"), "Complete"), new(new("b-return"), new("interaction"), new("return"), "Complete"),
            new(new("unfinished"), new("return"), new("next-draft-node"), "Unresolved target"), new(new("reconsider"), new("return"), new("entry"), "Review choice")]);
}
