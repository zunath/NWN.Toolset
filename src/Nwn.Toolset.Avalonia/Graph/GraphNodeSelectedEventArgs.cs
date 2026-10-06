namespace Nwn.Toolset.Avalonia.Graph;

public sealed class GraphNodeSelectedEventArgs(GraphNodeId? nodeId) : EventArgs
{
    public GraphNodeId? NodeId { get; } = nodeId;
}
