using System.Numerics;
using Nwn.Formats.Mdl;

namespace Nwn.Preview.Scene;

/// <summary>Combines placed scenes without sharing node or material identities between instances.</summary>
public static class SceneAssembler
{
    private const int MaximumInstances = 1024;
    private const int MaximumNodes = 100_000;
    private const int MaximumVertices = 4_000_000;
    private const int MaximumFaces = 4_000_000;

    public static PreparedScene Assemble(string name, IReadOnlyList<SceneInstance> instances)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(instances);
        if (name.Length > 128 || instances.Count > MaximumInstances)
            throw new ArgumentException("The scene name or instance count exceeds the assembly limit.");
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var nodes = new List<PreparedSceneNode>();
        long vertexCount = 0;
        long faceCount = 0;
        var hasAnimations = false;
        Vector3? minimumBounds = null;
        Vector3? maximumBounds = null;
        foreach (var instance in instances)
        {
            ArgumentNullException.ThrowIfNull(instance);
            ValidateId(instance.Id);
            ArgumentNullException.ThrowIfNull(instance.Scene);
            if (!ids.Add(instance.Id)) throw new ArgumentException("Scene instance identities must be unique.");
            var transform = instance.Transform;
            if (!IsFinite(transform) || transform.M14 != 0 || transform.M24 != 0 || transform.M34 != 0 || transform.M44 != 1
                || !Matrix4x4.Invert(transform, out var inverse) || !IsFinite(inverse))
                throw new ArgumentException("Scene instance transforms must be finite, affine and invertible.");
            if ((long)nodes.Count + instance.Scene.Nodes.Count > MaximumNodes)
                throw new ArgumentException("The assembled scene exceeds its node limit.");
            var normalTransform = Matrix4x4.Transpose(inverse);
            var mirrored = transform.GetDeterminant() < 0;
            hasAnimations |= instance.Scene.HasAnimations;
            foreach (var node in instance.Scene.Nodes)
            {
                PreparedMesh? mesh = null;
                if (node.Mesh is { } source)
                {
                    vertexCount += source.Vertices.Count;
                    faceCount += source.Faces.Count;
                    if (vertexCount > MaximumVertices || faceCount > MaximumFaces)
                        throw new ArgumentException("The assembled scene exceeds its geometry limit.");
                    var vertices = source.Vertices.Select(vertex => Vector3.Transform(vertex, transform)).ToArray();
                    foreach (var vertex in vertices)
                    {
                        minimumBounds = minimumBounds is { } lower
                            ? Vector3.Min(lower, vertex)
                            : vertex;
                        maximumBounds = maximumBounds is { } upper
                            ? Vector3.Max(upper, vertex)
                            : vertex;
                    }
                    var normals = source.Normals.Select(normal => Normalize(Vector3.TransformNormal(normal, normalTransform))).ToArray();
                    if (vertices.Any(vertex => !IsFinite(vertex)) || normals.Any(normal => !IsFinite(normal)))
                        throw new ArgumentException("The scene transform produces non-finite geometry.");
                    var faces = mirrored ? source.Faces.Select(Reverse).ToArray() : source.Faces;
                    mesh = new(vertices, normals, source.TextureVertices, faces,
                        ScopeNullable(instance.Id, source.BitmapName), ScopeNullable(instance.Id, source.MaterialName));
                }
                var world = node.WorldTransform * transform;
                if (!IsFinite(world)) throw new ArgumentException("The scene transform produces a non-finite node transform.");
                nodes.Add(new(ScopedName(instance.Id, node.Name), ScopeNullable(instance.Id, node.ParentName), world, node.RenderEnabled, mesh,
                    ScopedName(instance.Id, node.Id), ScopeNullable(instance.Id, node.ParentId)));
            }
        }
        var bounds = minimumBounds is { } minimum && maximumBounds is { } maximum
            ? new PreparedSceneBounds(minimum, maximum)
            : (PreparedSceneBounds?)null;
        return new(name, hasAnimations, nodes.ToArray(), bounds);
    }

    /// <summary>Qualifies the matching texture/material lookup key for an assembled instance.</summary>
    public static string ScopedName(string instanceId, string sourceName)
    {
        ValidateId(instanceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);
        return $"{instanceId}/{sourceName}";
    }

    private static string? ScopeNullable(string id, string? name) => name is null ? null : ScopedName(id, name);

    private static void ValidateId(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        if (id.Length > 128 || id.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not ('_' or '-')))
            throw new ArgumentException("Scene instance identities must be 1–128 ASCII letters, digits, underscores or hyphens.");
    }

    private static MdlTriangle Reverse(MdlTriangle face) => face with
    {
        VertexB = face.VertexC, VertexC = face.VertexB, TextureB = face.TextureC, TextureC = face.TextureB,
    };

    private static Vector3 Normalize(Vector3 normal)
    {
        if (!IsFinite(normal)) throw new ArgumentException("The scene transform produces a non-finite normal.");
        var maximum = MathF.Max(MathF.Abs(normal.X), MathF.Max(MathF.Abs(normal.Y), MathF.Abs(normal.Z)));
        return maximum > float.Epsilon ? Vector3.Normalize(normal / maximum) : Vector3.Zero;
    }

    private static bool IsFinite(Vector3 value) => float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);

    private static bool IsFinite(Matrix4x4 value) =>
        float.IsFinite(value.M11) && float.IsFinite(value.M12) && float.IsFinite(value.M13) && float.IsFinite(value.M14) &&
        float.IsFinite(value.M21) && float.IsFinite(value.M22) && float.IsFinite(value.M23) && float.IsFinite(value.M24) &&
        float.IsFinite(value.M31) && float.IsFinite(value.M32) && float.IsFinite(value.M33) && float.IsFinite(value.M34) &&
        float.IsFinite(value.M41) && float.IsFinite(value.M42) && float.IsFinite(value.M43) && float.IsFinite(value.M44);
}
