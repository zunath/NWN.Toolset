using System.Numerics;

namespace Nwn.Formats.Mdl;

/// <summary>One immutable model node with its source-local transform and optional triangle mesh.</summary>
public sealed class MdlNode
{
    public MdlNodeType Type { get; }
    public string Name { get; }
    public string? ParentName { get; }
    /// <summary>Hierarchy identity; compiled models can have distinct nodes with the same name.</summary>
    public string Id { get; }
    public string? ParentId { get; }
    public Vector3 Position { get; }
    /// <summary>Axis X, axis Y, axis Z, and angle in radians.</summary>
    public Vector4 Orientation { get; }
    public float Scale { get; }
    public bool RenderEnabled { get; }
    public MdlMesh? Mesh { get; }

    internal MdlNode(MdlNodeType type, string name, string? parentName, Vector3 position,
        Vector4 orientation, float scale, bool renderEnabled, MdlMesh? mesh, string? id = null, string? parentId = null)
    {
        Type = type;
        Name = name;
        ParentName = parentName;
        Id = id ?? name;
        ParentId = id is null ? parentName : parentId;
        Position = position;
        Orientation = orientation;
        Scale = scale;
        RenderEnabled = renderEnabled;
        Mesh = mesh;
    }
}
