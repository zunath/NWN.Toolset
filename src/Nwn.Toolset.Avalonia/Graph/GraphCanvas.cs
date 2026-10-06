using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Input;
using global::Avalonia.Media;
using global::Avalonia.Media.TextFormatting;

namespace Nwn.Toolset.Avalonia.Graph;

/// <summary>A directed graph with selectable nodes, movable layout, pan and anchored zoom.</summary>
public sealed class GraphCanvas : Control
{
    private static readonly IBrush BackgroundBrush = new SolidColorBrush(Color.Parse("#181E29"));
    private static readonly IBrush NodeBrush = new SolidColorBrush(Color.Parse("#293548"));
    private static readonly Pen EdgePen = new(new SolidColorBrush(Color.Parse("#91A3BC")), 1.5);
    private static readonly Pen MissingEdgePen = new(new SolidColorBrush(Color.Parse("#E9A269")), 1.5);
    private static readonly Pen SelectedPen = new(new SolidColorBrush(Color.Parse("#64B9FA")), 2);
    private readonly GraphViewportState _state = new();
    private Point _lastPointer;
    private GraphNodeId? _dragNode;
    private bool _panning;

    public GraphCanvas()
    {
        ClipToBounds = true;
        Focusable = true;
    }

    public GraphViewportState Viewport => _state;
    public event EventHandler<GraphNodeSelectedEventArgs>? NodeSelected;

    public void SetDocument(GraphDocument document)
    {
        _state.SetDocument(document);
        InvalidateVisual();
    }

    public void SelectNode(GraphNodeId? nodeId)
    {
        _state.Select(nodeId);
        InvalidateVisual();
    }

    public void Fit()
    {
        if (Bounds.Width <= 0 || Bounds.Height <= 0) return;
        _state.Fit(Bounds.Size);
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.DrawRectangle(BackgroundBrush, null, new Rect(Bounds.Size));
        if (_state.Document is not { } document) return;
        using var transform = context.PushTransform(Matrix.CreateScale(_state.Scale, _state.Scale) * Matrix.CreateTranslation(_state.Offset));
        var visible = new Rect(_state.ToGraph(default), _state.ToGraph(new Point(Bounds.Width, Bounds.Height)));
        for (var index = 0; index < document.Edges.Count; index++) DrawEdge(context, document.Edges[index], index, visible);
        foreach (var node in document.Nodes)
        {
            var position = _state.NodePositions[node.Id];
            var rectangle = new Rect(position, new Size(GraphViewportState.NodeWidth, GraphViewportState.NodeHeight));
            if (!visible.Intersects(rectangle)) continue;
            context.DrawRectangle(NodeBrush, _state.SelectedNode == node.Id ? SelectedPen : EdgePen, rectangle, 6, 6);
            DrawLabel(context, node.Title, position + new Vector(10, 8), 14, Brushes.White, 160, 25);
            DrawLabel(context, node.Subtitle, position + new Vector(10, 35), 11, Brushes.LightGray, 160, 25);
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs args)
    {
        base.OnPointerPressed(args);
        var properties = args.GetCurrentPoint(this).Properties;
        _lastPointer = args.GetPosition(this);
        _panning = properties.IsMiddleButtonPressed || properties.IsRightButtonPressed;
        if (!_panning && properties.IsLeftButtonPressed)
        {
            _dragNode = _state.HitTest(_lastPointer);
            _state.Select(_dragNode);
            NodeSelected?.Invoke(this, new GraphNodeSelectedEventArgs(_dragNode));
            _panning = _dragNode is null;
        }
        if (_panning || _dragNode is not null) args.Pointer.Capture(this);
        InvalidateVisual();
        args.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs args)
    {
        base.OnPointerMoved(args);
        if (args.Pointer.Captured != this) return;
        var position = args.GetPosition(this);
        var delta = new Vector(position.X - _lastPointer.X, position.Y - _lastPointer.Y);
        if (_panning) _state.Pan(delta);
        else if (_dragNode is { } node) _state.MoveNode(node, _state.NodePositions[node] + delta / _state.Scale);
        _lastPointer = position;
        InvalidateVisual();
        args.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs args)
    {
        base.OnPointerReleased(args);
        args.Pointer.Capture(null);
        _dragNode = null;
        _panning = false;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs args)
    {
        base.OnPointerWheelChanged(args);
        _state.ZoomAt(args.Delta.Y, args.GetPosition(this));
        InvalidateVisual();
        args.Handled = true;
    }

    private void DrawEdge(DrawingContext context, GraphEdge edge, int edgeIndex, Rect visible)
    {
        var source = _state.NodePositions[edge.Source];
        var from = source + new Vector(GraphViewportState.NodeWidth, GraphViewportState.NodeHeight / 2);
        var resolved = _state.NodePositions.TryGetValue(edge.Target, out var target);
        var to = resolved ? target + new Vector(0, GraphViewportState.NodeHeight / 2) : from + new Vector(90, 0);
        var pen = resolved ? EdgePen : MissingEdgePen;
        var direction = new Vector(to.X - from.X, to.Y - from.Y);
        var labelPosition = new Point((from.X + to.X) / 2 - 30, (from.Y + to.Y) / 2 - 24);
        var returning = resolved && target.X <= source.X;
        var top = Math.Min(source.Y, target.Y) - 40 - edgeIndex % 5 * 16;
        var extent = new Rect(new Point(Math.Min(from.X, to.X) - 20, returning ? top - 24 : Math.Min(from.Y, to.Y) - 24),
            new Point(Math.Max(from.X, to.X) + 20, Math.Max(from.Y, to.Y) + 20));
        if (!visible.Intersects(extent)) return;
        if (returning)
        {
            var exit = from + new Vector(20, 0);
            var entry = to - new Vector(20, 0);
            context.DrawLine(pen, from, exit);
            context.DrawLine(pen, exit, new Point(exit.X, top));
            context.DrawLine(pen, new Point(exit.X, top), new Point(entry.X, top));
            context.DrawLine(pen, new Point(entry.X, top), entry);
            context.DrawLine(pen, entry, to);
            direction = new Vector(1, 0);
            labelPosition = new Point((exit.X + entry.X) / 2 - 30, top - 24);
        }
        else context.DrawLine(pen, from, to);
        if (direction.Length > 0)
        {
            direction /= direction.Length;
            var normal = new Vector(-direction.Y, direction.X);
            context.DrawLine(pen, to, to - direction * 9 + normal * 4);
            context.DrawLine(pen, to, to - direction * 9 - normal * 4);
        }
        DrawLabel(context, edge.Label, labelPosition, 10,
            resolved ? Brushes.LightGray : Brushes.SandyBrown, 120, 22);
    }

    private static void DrawLabel(DrawingContext context, string text, Point position, double size, IBrush brush, double width, double height)
    {
        if (text.Length == 0) return;
        using var layout = new TextLayout(text, Typeface.Default, size, brush,
            textWrapping: TextWrapping.NoWrap, textTrimming: TextTrimming.CharacterEllipsis, maxWidth: width, maxHeight: height);
        layout.Draw(context, position);
    }
}
