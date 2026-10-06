using System.Collections.ObjectModel;
using System.Numerics;

namespace Nwn.Formats.Mdl;

/// <summary>An immutable triangle mesh with source vertex normals, UVs, faces, and texture names.</summary>
public sealed class MdlMesh
{
    public IReadOnlyList<Vector3> Vertices { get; }
    public IReadOnlyList<Vector3> Normals { get; }
    public IReadOnlyList<Vector2> TextureVertices { get; }
    public IReadOnlyList<MdlTriangle> Faces { get; }
    public string? BitmapName { get; }
    public string? MaterialName { get; }

    internal MdlMesh(Vector3[] vertices, Vector3[] normals, Vector2[] textureVertices,
        MdlTriangle[] faces, string? bitmapName, string? materialName)
    {
        Vertices = new ReadOnlyCollection<Vector3>(vertices.ToArray());
        Normals = new ReadOnlyCollection<Vector3>(normals.ToArray());
        TextureVertices = new ReadOnlyCollection<Vector2>(textureVertices.ToArray());
        Faces = new ReadOnlyCollection<MdlTriangle>(faces.ToArray());
        BitmapName = bitmapName;
        MaterialName = materialName;
    }
}
