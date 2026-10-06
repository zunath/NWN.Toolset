using System.Collections.ObjectModel;
using System.Numerics;
using Nwn.Formats.Mdl;

namespace Nwn.Preview.Scene;

/// <summary>A source mesh transformed into model-space positions and normals.</summary>
public sealed class PreparedMesh
{
    public IReadOnlyList<Vector3> Vertices { get; }
    public IReadOnlyList<Vector3> Normals { get; }
    public IReadOnlyList<Vector2> TextureVertices { get; }
    public IReadOnlyList<MdlTriangle> Faces { get; }
    public string? BitmapName { get; }
    public string? MaterialName { get; }

    internal PreparedMesh(Vector3[] vertices, Vector3[] normals, IReadOnlyList<Vector2> textureVertices,
        IReadOnlyList<MdlTriangle> faces, string? bitmapName, string? materialName)
    {
        Vertices = new ReadOnlyCollection<Vector3>(vertices);
        Normals = new ReadOnlyCollection<Vector3>(normals);
        TextureVertices = new ReadOnlyCollection<Vector2>(textureVertices.ToArray());
        Faces = new ReadOnlyCollection<MdlTriangle>(faces.ToArray());
        BitmapName = bitmapName;
        MaterialName = materialName;
    }
}
