using System.Numerics;
using Nwn.Formats.Mdl;

namespace Nwn.Preview.Scene;

/// <summary>Resolves static MDL local transforms and prepares immutable model-space meshes.</summary>
public static class MdlScenePreparer
{
    private const int MaximumHierarchyDepth = 128;

    public static PreparedScene Prepare(MdlScene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        var byId = scene.Nodes.ToDictionary(node => node.Id, StringComparer.OrdinalIgnoreCase);
        var worldById = new Dictionary<string, Matrix4x4>(StringComparer.OrdinalIgnoreCase);
        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var prepared = new PreparedSceneNode[scene.Nodes.Count];
        Vector3? minimumBounds = null;
        Vector3? maximumBounds = null;
        for (var i = 0; i < scene.Nodes.Count; i++)
        {
            var node = scene.Nodes[i];
            var world = ResolveWorldTransform(node, byId, worldById, visiting, 1);
            var mesh = node.Mesh is null
                ? null
                : PrepareMesh(node.Mesh, world, node.Name, ref minimumBounds, ref maximumBounds);
            prepared[i] = new PreparedSceneNode(node.Name, node.ParentName, world, node.RenderEnabled, mesh, node.Id, node.ParentId);
        }
        var bounds = minimumBounds is { } minimum && maximumBounds is { } maximum
            ? new PreparedSceneBounds(minimum, maximum)
            : (PreparedSceneBounds?)null;
        return new PreparedScene(scene.ModelName, scene.HasAnimations, prepared, bounds);
    }

    private static Matrix4x4 ResolveWorldTransform(MdlNode node, IReadOnlyDictionary<string, MdlNode> byId,
        IDictionary<string, Matrix4x4> worldById, ISet<string> visiting, int depth)
    {
        if (depth > MaximumHierarchyDepth)
            throw new FormatException($"MDL node hierarchy exceeds the preview preparation limit of {MaximumHierarchyDepth}.");
        if (worldById.TryGetValue(node.Id, out var world))
            return world;
        if (!visiting.Add(node.Id))
            throw new FormatException($"MDL node hierarchy contains a cycle at '{node.Name}'.");
        if (node.Scale <= 0)
            throw new FormatException($"MDL node '{node.Name}' scale must be positive for preview preparation.");

        var orientation = node.Orientation;
        var axis = new Vector3(orientation.X, orientation.Y, orientation.Z);
        Quaternion rotation;
        if (orientation.W == 0)
            rotation = Quaternion.Identity;
        else
        {
            var axisLengthSquared = axis.LengthSquared();
            if (!float.IsFinite(axisLengthSquared) || axisLengthSquared <= float.Epsilon)
                throw new FormatException($"MDL node '{node.Name}' has a nonzero orientation angle and a zero rotation axis.");
            rotation = Quaternion.CreateFromAxisAngle(Vector3.Normalize(axis), orientation.W);
        }
        var local = Matrix4x4.CreateScale(node.Scale) * Matrix4x4.CreateFromQuaternion(rotation) *
                    Matrix4x4.CreateTranslation(node.Position);
        if (node.ParentId is not null && !byId.TryGetValue(node.ParentId, out _))
            throw new FormatException($"MDL node '{node.Name}' refers to missing parent '{node.ParentName}'.");
        world = node.ParentId is null
            ? local
            : local * ResolveWorldTransform(byId[node.ParentId], byId, worldById, visiting, depth + 1);
        if (!IsFinite(world))
            throw new FormatException($"MDL node '{node.Name}' world transform exceeds finite numeric bounds.");
        visiting.Remove(node.Id);
        worldById.Add(node.Id, world);
        return world;
    }

    private static PreparedMesh PrepareMesh(
        MdlMesh source,
        Matrix4x4 world,
        string nodeName,
        ref Vector3? minimumBounds,
        ref Vector3? maximumBounds)
    {
        var vertices = new Vector3[source.Vertices.Count];
        for (var i = 0; i < vertices.Length; i++)
        {
            vertices[i] = Vector3.Transform(source.Vertices[i], world);
            if (!IsFinite(vertices[i]))
                throw new FormatException($"MDL node '{nodeName}' produces a non-finite transformed vertex.");
            minimumBounds = minimumBounds is { } minimum
                ? Vector3.Min(minimum, vertices[i])
                : vertices[i];
            maximumBounds = maximumBounds is { } maximum
                ? Vector3.Max(maximum, vertices[i])
                : vertices[i];
        }

        var normals = new Vector3[source.Normals.Count];
        if (normals.Length > 0)
        {
            if (!Matrix4x4.Invert(world, out var inverse))
                throw new FormatException($"MDL node '{nodeName}' has a non-invertible transform.");
            var normalTransform = Matrix4x4.Transpose(inverse);
            for (var i = 0; i < normals.Length; i++)
            {
                var normal = Vector3.TransformNormal(source.Normals[i], normalTransform);
                normals[i] = normal.LengthSquared() > float.Epsilon ? Vector3.Normalize(normal) : Vector3.Zero;
                if (!IsFinite(normals[i]))
                    throw new FormatException($"MDL node '{nodeName}' produces a non-finite transformed normal.");
            }
        }

        return new PreparedMesh(vertices, normals, source.TextureVertices, source.Faces,
            source.BitmapName, source.MaterialName);
    }

    private static bool IsFinite(Vector3 vector) => float.IsFinite(vector.X) && float.IsFinite(vector.Y) && float.IsFinite(vector.Z);

    private static bool IsFinite(Matrix4x4 matrix) =>
        float.IsFinite(matrix.M11) && float.IsFinite(matrix.M12) && float.IsFinite(matrix.M13) && float.IsFinite(matrix.M14) &&
        float.IsFinite(matrix.M21) && float.IsFinite(matrix.M22) && float.IsFinite(matrix.M23) && float.IsFinite(matrix.M24) &&
        float.IsFinite(matrix.M31) && float.IsFinite(matrix.M32) && float.IsFinite(matrix.M33) && float.IsFinite(matrix.M34) &&
        float.IsFinite(matrix.M41) && float.IsFinite(matrix.M42) && float.IsFinite(matrix.M43) && float.IsFinite(matrix.M44);
}
