using System.Collections.ObjectModel;
using global::Avalonia;

namespace Nwn.Toolset.Avalonia.Graph;

/// <summary>Diagram coordinates and selection independent of a platform renderer.</summary>
public sealed class GraphViewportState
{
    public const double NodeWidth = 180;
    public const double NodeHeight = 70;
    private Dictionary<GraphNodeId, Point> _positions = [];
    private IReadOnlyDictionary<GraphNodeId, Point> _readOnlyPositions = new ReadOnlyDictionary<GraphNodeId, Point>(new Dictionary<GraphNodeId, Point>());

    public GraphDocument? Document { get; private set; }
    public IReadOnlyDictionary<GraphNodeId, Point> NodePositions => _readOnlyPositions;
    public GraphNodeId? SelectedNode { get; private set; }
    public double Scale { get; private set; } = 1;
    public Vector Offset { get; private set; } = new(20, 20);

    public void SetDocument(GraphDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        Document = document;
        var arranged = DirectedGraphLayout.Arrange(document);
        _positions = arranged.ToDictionary(pair => pair.Key, pair => _positions.GetValueOrDefault(pair.Key, pair.Value));
        _readOnlyPositions = new ReadOnlyDictionary<GraphNodeId, Point>(_positions);
        if (SelectedNode is { } selected && !document.NodesById.ContainsKey(selected)) SelectedNode = null;
    }

    public void Select(GraphNodeId? id)
    {
        if (id is { } selected && (Document is null || !Document.NodesById.ContainsKey(selected)))
            throw new ArgumentException("Selection must name a visible node.", nameof(id));
        SelectedNode = id;
    }

    public void MoveNode(GraphNodeId id, Point position)
    {
        if (!_positions.ContainsKey(id)) throw new ArgumentException("The node is absent from the diagram.", nameof(id));
        if (!double.IsFinite(position.X) || !double.IsFinite(position.Y) || Math.Abs(position.X) > 10_000_000 || Math.Abs(position.Y) > 10_000_000)
            throw new ArgumentOutOfRangeException(nameof(position));
        _positions[id] = position;
    }

    public Point ToGraph(Point screen) => new((screen.X - Offset.X) / Scale, (screen.Y - Offset.Y) / Scale);
    public Point ToScreen(Point graph) => new(graph.X * Scale + Offset.X, graph.Y * Scale + Offset.Y);

    public GraphNodeId? HitTest(Point screen)
    {
        var point = ToGraph(screen);
        foreach (var pair in _positions.Reverse())
            if (new Rect(pair.Value, new Size(NodeWidth, NodeHeight)).Contains(point)) return pair.Key;
        return null;
    }

    public void Pan(Vector delta)
    {
        if (!double.IsFinite(delta.X) || !double.IsFinite(delta.Y)) throw new ArgumentOutOfRangeException(nameof(delta));
        Offset = new Vector(Math.Clamp(Offset.X + delta.X, -10_000_000, 10_000_000), Math.Clamp(Offset.Y + delta.Y, -10_000_000, 10_000_000));
    }

    public void ZoomAt(double delta, Point screen)
    {
        if (!double.IsFinite(delta) || !double.IsFinite(screen.X) || !double.IsFinite(screen.Y) || Math.Abs(screen.X) > 10_000_000 || Math.Abs(screen.Y) > 10_000_000)
            throw new ArgumentOutOfRangeException(nameof(delta));
        var anchor = ToGraph(screen);
        Scale = Math.Clamp(Scale * Math.Exp(Math.Clamp(delta, -20, 20) * 0.15), 0.1, 2);
        Offset = new Vector(screen.X - anchor.X * Scale, screen.Y - anchor.Y * Scale);
    }

    public void Fit(Size viewport)
    {
        if (!double.IsFinite(viewport.Width) || !double.IsFinite(viewport.Height) || viewport.Width <= 0 || viewport.Height <= 0 || viewport.Width > 10_000_000 || viewport.Height > 10_000_000)
            throw new ArgumentOutOfRangeException(nameof(viewport));
        if (_positions.Count == 0) { Scale = 1; Offset = new Vector(20, 20); return; }
        var left = _positions.Values.Min(point => point.X) - 20;
        var top = _positions.Values.Min(point => point.Y);
        var right = _positions.Values.Max(point => point.X) + NodeWidth + 20;
        foreach (var edge in Document!.Edges)
        {
            if (!_positions.TryGetValue(edge.Target, out var target)) right = Math.Max(right, _positions[edge.Source].X + NodeWidth + 150);
            else if (target.X <= _positions[edge.Source].X) top = Math.Min(top, Math.Min(target.Y, _positions[edge.Source].Y) - 128);
        }
        var width = right - left;
        var height = _positions.Values.Max(point => point.Y) + NodeHeight - top;
        Scale = Math.Clamp(Math.Min(Math.Max(1, viewport.Width - 40) / width, Math.Max(1, viewport.Height - 40) / height), 0.1, 2);
        Offset = new Vector((viewport.Width - width * Scale) / 2 - left * Scale, (viewport.Height - height * Scale) / 2 - top * Scale);
    }
}
