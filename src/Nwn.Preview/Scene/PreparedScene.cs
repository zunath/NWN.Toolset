using System.Collections.ObjectModel;

namespace Nwn.Preview.Scene;

/// <summary>Prepared static geometry and source animation-presence metadata.</summary>
public sealed class PreparedScene
{
    public string ModelName { get; }
    public bool HasAnimations { get; }
    public bool AnimationTracksApplied => false;
    public IReadOnlyList<PreparedSceneNode> Nodes { get; }
    public PreparedSceneBounds? Bounds { get; }

    internal PreparedScene(string modelName, bool hasAnimations, PreparedSceneNode[] nodes, PreparedSceneBounds? bounds)
    {
        ModelName = modelName;
        HasAnimations = hasAnimations;
        Nodes = new ReadOnlyCollection<PreparedSceneNode>(nodes.ToArray());
        Bounds = bounds;
    }
}
