using System.Buffers.Binary;
using System.Numerics;
using System.Text;

namespace Nwn.Formats.Mdl;

/// <summary>Reads rigid compiled NWN MDL meshes. Model and node animation tracks are reported but not evaluated.</summary>
public static class MdlBinaryReader
{
    private const int ModelDataOffset = 12;
    private const int ModelHeaderLength = 232;
    private const int NodeHeaderLength = 112;
    private const int MeshHeaderLength = 512;
    private const int MeshContent = 1 << 5;
    private const int SkinContent = 1 << 6;
    private const int AnimContent = 1 << 7;
    private const int DanglyContent = 1 << 8;
    private const int AabbContent = 1 << 9;
    private const int HeaderContent = 1;
    private const int MaximumArrayCapacity = 2_000_000;

    public static MdlScene Read(ReadOnlySpan<byte> bytes, MdlBinaryReadOptions? options = null)
    {
        options ??= new MdlBinaryReadOptions();
        options.Validate();
        if (bytes.Length > options.MaximumInputBytes)
            throw new FormatException($"Binary MDL input size {bytes.Length} exceeds the configured limit {options.MaximumInputBytes}.");
        if (bytes.Length < ModelDataOffset + ModelHeaderLength || U32(bytes, 0) != 0)
            throw new FormatException("Input is not a complete compiled NWN MDL file.");

        var mdxOffset = U32(bytes, 4);
        var mdxLength = U32(bytes, 8);
        if (mdxOffset > int.MaxValue || mdxLength > int.MaxValue || ModelDataOffset + (long)mdxOffset + mdxLength > bytes.Length)
            throw new FormatException("Compiled MDL embedded MDX region is outside the input bounds.");
        var mdxBase = checked(ModelDataOffset + (int)mdxOffset);
        var modelName = Name(bytes, ModelDataOffset + 8, 64);
        var rootOffset = I32(bytes, ModelDataOffset + 72);
        var declaredNodeCount = I32(bytes, ModelDataOffset + 76);
        if (string.IsNullOrWhiteSpace(modelName) || declaredNodeCount < 1 || declaredNodeCount > options.MaximumNodeCount)
            throw new FormatException("Compiled MDL model name or declared node count is invalid.");
        var animationOffset = U32(bytes, ModelDataOffset + 120);
        var animationCount = U32(bytes, ModelDataOffset + 124);
        if (animationCount > options.MaximumNodeCount)
            throw new FormatException("Compiled MDL animation count exceeds the configured bound.");
        if (animationCount > 0 && (animationOffset > int.MaxValue || (long)animationOffset + animationCount * 4L > bytes.Length - ModelDataOffset))
            throw new FormatException("Compiled MDL animation pointer array exceeds the model-data bounds.");
        var hasAnimations = animationCount > 0;

        var parser = new Parser(bytes.ToArray(), mdxBase, (int)mdxLength, options);
        var nodes = new List<MdlNode>(declaredNodeCount);
        var visited = new HashSet<int>();
        parser.ReadNode(checked(ModelDataOffset + rootOffset), null, null, nodes, visited, ref hasAnimations, 1);
        // The compiler numbers this model's nodes after the inherited supermodel's
        // nodes. The header therefore bounds the local hierarchy rather than counting it.
        if (nodes.Count > declaredNodeCount)
            throw new FormatException($"Compiled MDL node count {declaredNodeCount} cannot contain its {nodes.Count}-node hierarchy.");
        return new MdlScene(modelName, nodes.ToArray(), hasAnimations);
    }

    private sealed class Parser(byte[] data, int mdxBase, int mdxLength, MdlBinaryReadOptions options)
    {
        private readonly byte[] _data = data;
        private int _vertexCount;
        private int _faceCount;

        public void ReadNode(int offset, string? parentName, string? parentId, List<MdlNode> nodes, ISet<int> visited,
            ref bool hasAnimations, int depth)
        {
            if (depth > options.MaximumHierarchyDepth)
                throw new FormatException($"Compiled MDL hierarchy depth exceeds the configured limit {options.MaximumHierarchyDepth}.");
            if (nodes.Count >= options.MaximumNodeCount || !visited.Add(offset))
                throw new FormatException("Compiled MDL node hierarchy is cyclic, shared, or exceeds the configured node limit.");
            Bounds(offset, NodeHeaderLength, "node header");
            var nodeName = Name(_data, offset + 32, 32);
            if (nodeName.Length == 0)
                throw new FormatException("Compiled MDL contains an empty node name.");
            var nodeId = offset.ToString("X", System.Globalization.CultureInfo.InvariantCulture);

            var children = ArrayDefinition(offset + 72, "child node");
            var controllerKeys = ArrayDefinition(offset + 84, "controller key", 12);
            var controllerData = ArrayDefinition(offset + 96, "controller data", 4);
            var flags = checked((int)U32(_data, offset + 108));
            if ((flags & HeaderContent) == 0 || (flags & (SkinContent | DanglyContent | AabbContent | AnimContent)) != 0 ||
                (flags & ~(HeaderContent | MeshContent)) != 0)
                throw new NotSupportedException($"Compiled MDL node '{nodeName}' uses unsupported content flags 0x{flags:X}.");

            var position = Vector3.Zero;
            var orientation = Vector4.Zero;
            var scale = 1f;
            ReadStaticTransforms(controllerKeys, controllerData, nodeName, ref position, ref orientation, ref scale, ref hasAnimations);

            MdlMesh? mesh = null;
            var render = true;
            if ((flags & MeshContent) != 0)
            {
                var geometryOffset = offset + NodeHeaderLength;
                Bounds(geometryOffset, MeshHeaderLength, "mesh header");
                mesh = ReadMesh(geometryOffset, nodeName);
                render = U32(_data, geometryOffset + 108) != 0;
            }

            nodes.Add(new MdlNode(mesh is null ? MdlNodeType.Dummy : MdlNodeType.Trimesh, nodeName,
                parentName, position, orientation, scale, render, mesh, nodeId, parentId));
            for (var i = 0; i < children.Count; i++)
            {
                var childOffset = checked(ModelDataOffset + I32(_data, checked((int)children.Offset + i * 4)));
                ReadNode(childOffset, nodeName, nodeId, nodes, visited, ref hasAnimations, depth + 1);
            }
        }

        private MdlMesh ReadMesh(int offset, string nodeName)
        {
            var faceArray = ArrayDefinition(offset + 8, "mesh face", 32);
            var vertexCount = U16(_data, offset + 448);
            if (vertexCount == 0 || vertexCount > options.MaximumVertexCount - _vertexCount)
                throw new FormatException($"Compiled MDL mesh '{nodeName}' vertex count is zero or exceeds the configured bound.");
            _vertexCount += vertexCount;
            if (faceArray.Count == 0 || faceArray.Count > options.MaximumFaceCount - _faceCount)
                throw new FormatException($"Compiled MDL mesh '{nodeName}' face count is zero or exceeds the configured bound.");
            _faceCount += faceArray.Count;

            var meshType = I32(_data, offset + 436);
            if (meshType != 3)
                throw new NotSupportedException($"Compiled MDL mesh '{nodeName}' uses mesh type {meshType}; only triangle lists are supported.");
            var vertexPointer = I32(_data, offset + 444);
            var texturePointer = I32(_data, offset + 452);
            if (I32(_data, offset + 456) != -1 || I32(_data, offset + 460) != -1 || I32(_data, offset + 464) != -1)
                throw new NotSupportedException($"Compiled MDL mesh '{nodeName}' uses additional texture-coordinate streams.");
            var normalPointer = I32(_data, offset + 468);
            var vertices = ReadVector3Stream(vertexPointer, vertexCount, "vertex");
            var normals = normalPointer == -1 ? [] : ReadVector3Stream(normalPointer, vertexCount, "normal");
            var textureVertices = texturePointer == -1 ? [] : ReadVector2Stream(texturePointer, vertexCount, "texture coordinate");
            var faces = new MdlTriangle[faceArray.Count];
            for (var i = 0; i < faces.Length; i++)
            {
                var face = checked((int)faceArray.Offset + i * 32);
                var a = U16(_data, face + 26);
                var b = U16(_data, face + 28);
                var c = U16(_data, face + 30);
                if (a >= vertexCount || b >= vertexCount || c >= vertexCount)
                    throw new FormatException($"Compiled MDL mesh '{nodeName}' face {i} references a vertex outside its array.");
                faces[i] = new MdlTriangle(a, b, c, 0, a, b, c, 0);
            }

            var bitmap = Name(_data, offset + 120, 64);
            var material = Name(_data, offset + 312, 64);
            return new MdlMesh(vertices, normals, textureVertices, faces,
                bitmap.Length == 0 ? null : bitmap, material.Length == 0 ? null : material);
        }

        private void ReadStaticTransforms(ArrayInfo keys, ArrayInfo values, string nodeName,
            ref Vector3 position, ref Vector4 orientation, ref float scale, ref bool hasAnimations)
        {
            for (var i = 0; i < keys.Count; i++)
            {
                var key = checked((int)keys.Offset + i * 12);
                var controllerType = I32(_data, key);
                var valueCount = U16(_data, key + 4);
                var dataStart = U16(_data, key + 8);
                var columns = _data[key + 10];
                var expectedColumns = controllerType switch { 8 => 3, 20 => 4, 36 => 1, _ => 0 };
                if (expectedColumns != 0 && columns != expectedColumns)
                    throw new FormatException($"Compiled MDL node '{nodeName}' transform controller {controllerType} has {columns} columns, expected {expectedColumns}.");
                if (valueCount > 1)
                {
                    hasAnimations = true;
                    continue;
                }
                if (controllerType is 8 or 20 or 36 && valueCount != 1)
                    throw new FormatException($"Compiled MDL node '{nodeName}' has an empty transform controller.");
                switch (controllerType)
                {
                    case 8:
                        position = new Vector3(Value(dataStart), Value(dataStart + 1), Value(dataStart + 2));
                        break;
                    case 20:
                        var q = new Quaternion(Value(dataStart), Value(dataStart + 1), Value(dataStart + 2), Value(dataStart + 3));
                        if (q.LengthSquared() > float.Epsilon)
                        {
                            q = Quaternion.Normalize(q);
                            var angle = 2f * MathF.Acos(Math.Clamp(q.W, -1f, 1f));
                            var sin = MathF.Sqrt(Math.Max(0, 1f - q.W * q.W));
                            orientation = sin < 1e-6f ? Vector4.Zero : new Vector4(q.X / sin, q.Y / sin, q.Z / sin, angle);
                        }
                        break;
                    case 36:
                        scale = Value(dataStart);
                        break;
                }
            }

            float Value(int index)
            {
                if (index < 0 || index >= values.Count)
                    throw new FormatException($"Compiled MDL node '{nodeName}' transform controller points outside its data array.");
                var value = F32(_data, checked((int)values.Offset + index * 4));
                if (!float.IsFinite(value))
                    throw new FormatException($"Compiled MDL node '{nodeName}' contains a non-finite transform value.");
                return value;
            }
        }

        private Vector3[] ReadVector3Stream(int pointer, int count, string kind)
        {
            var start = MdxStream(pointer, count, 12, kind);
            var result = new Vector3[count];
            for (var i = 0; i < count; i++)
            {
                var offset = start + i * 12;
                result[i] = new Vector3(Finite(offset), Finite(offset + 4), Finite(offset + 8));
            }
            return result;
        }

        private Vector2[] ReadVector2Stream(int pointer, int count, string kind)
        {
            var start = MdxStream(pointer, count, 8, kind);
            var result = new Vector2[count];
            for (var i = 0; i < count; i++)
            {
                var offset = start + i * 8;
                result[i] = new Vector2(Finite(offset), Finite(offset + 4));
            }
            return result;
        }

        private int MdxStream(int pointer, int count, int stride, string kind)
        {
            if (pointer < 0)
                throw new FormatException($"Compiled MDL {kind} stream is absent.");
            var end = (long)pointer + (long)count * stride;
            if (end > mdxLength)
                throw new FormatException($"Compiled MDL {kind} stream exceeds the embedded MDX bounds.");
            return mdxBase + pointer;
        }

        private float Finite(int offset)
        {
            var value = F32(_data, offset);
            if (!float.IsFinite(value))
                throw new FormatException("Compiled MDL geometry contains a non-finite value.");
            return value;
        }

        private ArrayInfo ArrayDefinition(int offset, string kind, int stride = 4)
        {
            Bounds(offset, 12, $"{kind} array definition");
            var pointer = I32(_data, offset);
            var count = I32(_data, offset + 4);
            var capacity = I32(_data, offset + 8);
            if (count < 0 || capacity < count || capacity > MaximumArrayCapacity)
                throw new FormatException($"Compiled MDL {kind} array count/capacity is invalid.");
            if (count == 0)
                return new ArrayInfo(0, 0);
            var modelDataLength = mdxBase - ModelDataOffset;
            if (pointer < 0 || (long)pointer + (long)count * stride > modelDataLength)
                throw new FormatException($"Compiled MDL {kind} array exceeds the model-data bounds.");
            return new ArrayInfo(checked(ModelDataOffset + pointer), count);
        }

        private void Bounds(int offset, int length, string description)
        {
            if (offset < 0 || (long)offset + length > mdxBase)
                throw new FormatException($"Compiled MDL {description} at model-data offset {offset} exceeds the file bounds.");
        }
    }

    private readonly record struct ArrayInfo(int Offset, int Count);

    private static uint U32(ReadOnlySpan<byte> data, int offset) => BinaryPrimitives.ReadUInt32LittleEndian(Slice(data, offset, 4));
    private static int I32(ReadOnlySpan<byte> data, int offset) => BinaryPrimitives.ReadInt32LittleEndian(Slice(data, offset, 4));
    private static ushort U16(ReadOnlySpan<byte> data, int offset) => BinaryPrimitives.ReadUInt16LittleEndian(Slice(data, offset, 2));
    private static float F32(ReadOnlySpan<byte> data, int offset) => BitConverter.Int32BitsToSingle(I32(data, offset));

    private static ReadOnlySpan<byte> Slice(ReadOnlySpan<byte> data, int offset, int count)
    {
        if (offset < 0 || (long)offset + count > data.Length)
            throw new FormatException($"Compiled MDL is truncated at byte offset {offset}.");
        return data.Slice(offset, count);
    }

    private static string Name(ReadOnlySpan<byte> data, int offset, int count)
    {
        var field = Slice(data, offset, count);
        var end = field.IndexOf((byte)0);
        return Encoding.ASCII.GetString(end < 0 ? field : field[..end]);
    }
}
