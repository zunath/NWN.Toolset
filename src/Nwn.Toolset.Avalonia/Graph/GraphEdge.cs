namespace Nwn.Toolset.Avalonia.Graph;

public sealed record GraphEdge(GraphEdgeId Id, GraphNodeId Source, GraphNodeId Target, string Label = "");
