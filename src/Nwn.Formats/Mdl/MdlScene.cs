using System.Collections.ObjectModel;

namespace Nwn.Formats.Mdl;

/// <summary>An immutable ASCII MDL model's static geometry, retaining animation presence separately.</summary>
public sealed class MdlScene
{
    public string ModelName { get; }
    public IReadOnlyList<MdlNode> Nodes { get; }
    public bool HasAnimations { get; }

    internal MdlScene(string modelName, MdlNode[] nodes, bool hasAnimations)
    {
        ModelName = modelName;
        Nodes = new ReadOnlyCollection<MdlNode>(nodes.ToArray());
        HasAnimations = hasAnimations;
    }
}
