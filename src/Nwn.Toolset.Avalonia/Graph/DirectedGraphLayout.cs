using global::Avalonia;

namespace Nwn.Toolset.Avalonia.Graph;

/// <summary>Places every node deterministically, including cyclic and disconnected components.</summary>
internal static class DirectedGraphLayout
{
    public static Dictionary<GraphNodeId, Point> Arrange(GraphDocument document)
    {
        var outgoing = document.Nodes.ToDictionary(node => node.Id, _ => new List<GraphNodeId>());
        var incoming = document.Nodes.ToDictionary(node => node.Id, _ => 0);
        foreach (var edge in document.Edges)
        {
            if (!incoming.ContainsKey(edge.Target)) continue;
            outgoing[edge.Source].Add(edge.Target);
            incoming[edge.Target]++;
        }
        var positions = new Dictionary<GraphNodeId, Point>();
        var rows = new Dictionary<int, int>();
        var queue = new Queue<(GraphNodeId Id, int Column)>();
        foreach (var node in document.Nodes.Where(node => incoming[node.Id] == 0)) queue.Enqueue((node.Id, 0));
        var unplaced = 0;
        while (positions.Count < document.Nodes.Count)
        {
            if (queue.Count == 0)
            {
                while (positions.ContainsKey(document.Nodes[unplaced].Id)) unplaced++;
                queue.Enqueue((document.Nodes[unplaced].Id, 0));
            }
            var (id, column) = queue.Dequeue();
            if (positions.ContainsKey(id)) continue;
            rows.TryGetValue(column, out var row);
            positions.Add(id, new Point(column * 280, row * 110));
            rows[column] = row + 1;
            foreach (var target in outgoing[id])
                if (!positions.ContainsKey(target)) queue.Enqueue((target, column + 1));
        }
        return positions;
    }
}
