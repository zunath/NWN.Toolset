using System.Numerics;

namespace Nwn.Preview.Scene;

/// <summary>A prepared node that retains its hierarchy reference and resolved model-space transform.</summary>
public sealed class PreparedSceneNode
{
    public string Name { get; }
    public string? ParentName { get; }
    public string Id { get; }
    public string? ParentId { get; }
    public Matrix4x4 WorldTransform { get; }
    public bool RenderEnabled { get; }
    public PreparedMesh? Mesh { get; }

    internal PreparedSceneNode(string name, string? parentName, Matrix4x4 worldTransform,
        bool renderEnabled, PreparedMesh? mesh, string? id = null, string? parentId = null)
    {
        Name = name;
        ParentName = parentName;
        Id = id ?? name;
        ParentId = id is null ? parentName : parentId;
        WorldTransform = worldTransform;
        RenderEnabled = renderEnabled;
        Mesh = mesh;
    }
}
