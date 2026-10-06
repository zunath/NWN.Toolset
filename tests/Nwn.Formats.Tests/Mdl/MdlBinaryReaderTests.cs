using System.Buffers.Binary;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Formats.Mdl;

namespace Nwn.Formats.Tests.Mdl;

[TestClass]
public sealed class MdlBinaryReaderTests
{
    [TestMethod]
    public void Reader_PreservesRigidHierarchyGeometryAndStaticControllerTransform()
    {
        var scene = MdlBinaryReader.Read(CreateRigidModel());

        Assert.AreEqual("fixture", scene.ModelName);
        Assert.IsFalse(scene.HasAnimations, "Single-key node controllers establish static transforms, not animated tracks.");
        Assert.AreEqual(2, scene.Nodes.Count);
        var meshNode = scene.Nodes[1];
        Assert.AreEqual("mesh", meshNode.Name);
        Assert.AreEqual("root", meshNode.ParentName);
        Assert.AreEqual(new Vector3(2, 3, 4), meshNode.Position);
        Assert.AreEqual(MdlNodeType.Trimesh, meshNode.Type);
        Assert.AreEqual("unit_texture", meshNode.Mesh!.BitmapName);
        Assert.AreEqual("unit_material", meshNode.Mesh.MaterialName);
        Assert.AreEqual(3, meshNode.Mesh.Vertices.Count);
        Assert.AreEqual(3, meshNode.Mesh.Normals.Count);
        Assert.AreEqual(3, meshNode.Mesh.TextureVertices.Count);
        Assert.AreEqual(new Vector2(1, 1), meshNode.Mesh.TextureVertices[2]);
        Assert.AreEqual(new MdlTriangle(0, 1, 2, 0, 0, 1, 2, 0), meshNode.Mesh.Faces[0]);
    }

    [TestMethod]
    public void Reader_AllowsInheritedNodeNumbersButRejectsUnderdeclaredAndUnboundedCounts()
    {
        var inherited = CreateRigidModel();
        WriteInt32(inherited, 12 + 76, 329);
        WriteName(inherited, 12 + 168, 64, "supermodel");
        Assert.AreEqual(2, MdlBinaryReader.Read(inherited).Nodes.Count);

        var underdeclared = CreateRigidModel();
        WriteInt32(underdeclared, 12 + 76, 1);
        Assert.ThrowsExactly<FormatException>(() => MdlBinaryReader.Read(underdeclared));

        var unbounded = CreateRigidModel();
        WriteInt32(unbounded, 12 + 76, 4097);
        Assert.ThrowsExactly<FormatException>(() => MdlBinaryReader.Read(unbounded));
        Assert.ThrowsExactly<FormatException>(() => MdlBinaryReader.Read(inherited,
            new MdlBinaryReadOptions { MaximumNodeCount = 328 }));
    }

    [TestMethod]
    public void Reader_PreservesDuplicateNativeNamesWithDistinctPointerIdentities()
    {
        var bytes = CreateRigidModel();
        WriteName(bytes, 12 + 344 + 32, 32, "root");
        var scene = MdlBinaryReader.Read(bytes);
        Assert.AreEqual("root", scene.Nodes[0].Name);
        Assert.AreEqual("root", scene.Nodes[1].Name);
        Assert.AreNotEqual(scene.Nodes[0].Id, scene.Nodes[1].Id);
        Assert.IsNull(scene.Nodes[0].ParentId);
        Assert.AreEqual(scene.Nodes[0].Id, scene.Nodes[1].ParentId);
    }

    [TestMethod]
    public void Reader_InterpretsCompiledQuaternionComponentsAsXyzW()
    {
        var bytes = CreateRigidModel();
        bytes.AsSpan(12 + 1080, 12).CopyTo(bytes.AsSpan(12 + 1064, 12));
        WriteInt32(bytes, 12 + 344 + 84, 1064);
        WriteInt32(bytes, 12 + 344 + 88, 2);
        WriteInt32(bytes, 12 + 344 + 92, 2);
        WriteInt32(bytes, 12 + 344 + 100, 8);
        WriteInt32(bytes, 12 + 344 + 104, 8);
        WriteInt32(bytes, 12 + 1076, 20);
        WriteUInt16(bytes, 12 + 1076 + 4, 1);
        WriteUInt16(bytes, 12 + 1076 + 8, 4);
        bytes[12 + 1076 + 10] = 4;
        WriteFloat(bytes, 12 + 1116, 0);
        WriteFloat(bytes, 12 + 1120, 0);
        WriteFloat(bytes, 12 + 1124, 0);
        WriteFloat(bytes, 12 + 1128, 1);
        Assert.AreEqual(Vector4.Zero, MdlBinaryReader.Read(bytes).Nodes[1].Orientation);
        WriteFloat(bytes, 12 + 1124, MathF.Sin(MathF.PI / 4));
        WriteFloat(bytes, 12 + 1128, MathF.Cos(MathF.PI / 4));
        var rotation = MdlBinaryReader.Read(bytes).Nodes[1].Orientation;
        Assert.AreEqual(0, rotation.X, 0.00001f);
        Assert.AreEqual(0, rotation.Y, 0.00001f);
        Assert.AreEqual(1, rotation.Z, 0.00001f);
        Assert.AreEqual(MathF.PI / 2, rotation.W, 0.00001f);
    }

    [TestMethod]
    public void Reader_RejectsTruncationUnsupportedDanglyAndOutOfRangeStreams()
    {
        var model = CreateRigidModel();
        Assert.ThrowsExactly<FormatException>(() => MdlBinaryReader.Read(model.AsSpan(0, model.Length - 1)));

        var badStream = CreateRigidModel();
        WriteInt32(badStream, 12 + 456 + 444, 105);
        Assert.ThrowsExactly<FormatException>(() => MdlBinaryReader.Read(badStream));

        var dangly = CreateRigidModel();
        WriteUInt32(dangly, 12 + 344 + 108, 1u | 1u << 5 | 1u << 8);
        Assert.ThrowsExactly<NotSupportedException>(() => MdlBinaryReader.Read(dangly));
    }

    [TestMethod]
    public void Reader_ReportsMultiValueControllerTracksWithoutApplyingThem()
    {
        var model = CreateRigidModel();
        WriteUInt16(model, 12 + 1080 + 4, 2);

        var scene = MdlBinaryReader.Read(model);

        Assert.IsTrue(scene.HasAnimations);
        Assert.AreEqual(Vector3.Zero, scene.Nodes[1].Position);
    }

    [TestMethod]
    public void Reader_RejectsOverdeepHierarchyMdXOverlapsAndMalformedTransformShape()
    {
        var model = CreateRigidModel();
        Assert.ThrowsExactly<FormatException>(() => MdlBinaryReader.Read(model,
            new MdlBinaryReadOptions { MaximumHierarchyDepth = 1 }));

        var faceArrayInMdx = CreateRigidModel();
        WriteInt32(faceArrayInMdx, 12 + 344 + 112 + 8, 1200);
        Assert.ThrowsExactly<FormatException>(() => MdlBinaryReader.Read(faceArrayInMdx));

        var controllerArrayInMdx = CreateRigidModel();
        WriteInt32(controllerArrayInMdx, 12 + 344 + 96, 1199);
        Assert.ThrowsExactly<FormatException>(() => MdlBinaryReader.Read(controllerArrayInMdx));

        var malformedController = CreateRigidModel();
        malformedController[12 + 1080 + 10] = 2;
        Assert.ThrowsExactly<FormatException>(() => MdlBinaryReader.Read(malformedController));
    }

    private static byte[] CreateRigidModel()
    {
        const int mdxOffset = 1200;
        const int mdxLength = 96;
        var bytes = new byte[12 + mdxOffset + mdxLength];
        WriteUInt32(bytes, 0, 0);
        WriteUInt32(bytes, 4, mdxOffset);
        WriteUInt32(bytes, 8, mdxLength);
        WriteName(bytes, 12 + 8, 64, "fixture");
        WriteInt32(bytes, 12 + 72, 232);
        WriteInt32(bytes, 12 + 76, 2);

        var root = 12 + 232;
        WriteName(bytes, root + 32, 32, "root");
        WriteInt32(bytes, root + 72, 980);
        WriteInt32(bytes, root + 76, 1);
        WriteInt32(bytes, root + 80, 1);
        WriteUInt32(bytes, root + 108, 1);
        WriteInt32(bytes, 12 + 980, 344);

        var node = 12 + 344;
        WriteName(bytes, node + 32, 32, "mesh");
        WriteInt32(bytes, node + 68, 232);
        WriteInt32(bytes, node + 84, 1080);
        WriteInt32(bytes, node + 88, 1);
        WriteInt32(bytes, node + 92, 1);
        WriteInt32(bytes, node + 96, 1100);
        WriteInt32(bytes, node + 100, 4);
        WriteInt32(bytes, node + 104, 4);
        WriteUInt32(bytes, node + 108, 1u | 1u << 5);

        var mesh = node + 112;
        WriteInt32(bytes, mesh + 8, 1000);
        WriteInt32(bytes, mesh + 12, 1);
        WriteInt32(bytes, mesh + 16, 1);
        WriteUInt32(bytes, mesh + 108, 1);
        WriteName(bytes, mesh + 120, 64, "unit_texture");
        WriteName(bytes, mesh + 312, 64, "unit_material");
        WriteInt32(bytes, mesh + 436, 3);
        WriteInt32(bytes, mesh + 444, 0);
        WriteUInt16(bytes, mesh + 448, 3);
        WriteUInt16(bytes, mesh + 450, 1);
        WriteInt32(bytes, mesh + 452, 36);
        WriteInt32(bytes, mesh + 456, -1);
        WriteInt32(bytes, mesh + 460, -1);
        WriteInt32(bytes, mesh + 464, -1);
        WriteInt32(bytes, mesh + 468, 60);
        var face = 12 + 1000;
        WriteUInt16(bytes, face + 26, 0);
        WriteUInt16(bytes, face + 28, 1);
        WriteUInt16(bytes, face + 30, 2);

        var key = 12 + 1080;
        WriteInt32(bytes, key, 8);
        WriteUInt16(bytes, key + 4, 1);
        WriteUInt16(bytes, key + 6, 0);
        WriteUInt16(bytes, key + 8, 1);
        bytes[key + 10] = 3;
        WriteFloat(bytes, 12 + 1100, 0);
        WriteFloat(bytes, 12 + 1104, 2);
        WriteFloat(bytes, 12 + 1108, 3);
        WriteFloat(bytes, 12 + 1112, 4);

        var mdx = 12 + mdxOffset;
        WriteVector3(bytes, mdx, new Vector3(0, 0, 0));
        WriteVector3(bytes, mdx + 12, new Vector3(1, 0, 0));
        WriteVector3(bytes, mdx + 24, new Vector3(0, 1, 0));
        WriteVector2(bytes, mdx + 36, new Vector2(0, 0));
        WriteVector2(bytes, mdx + 44, new Vector2(1, 0));
        WriteVector2(bytes, mdx + 52, new Vector2(1, 1));
        WriteVector3(bytes, mdx + 60, Vector3.UnitZ);
        WriteVector3(bytes, mdx + 72, Vector3.UnitZ);
        return bytes;
    }

    private static void WriteName(byte[] bytes, int offset, int length, string value)
    {
        var encoded = System.Text.Encoding.ASCII.GetBytes(value);
        if (encoded.Length >= length)
            throw new ArgumentOutOfRangeException(nameof(value));
        encoded.CopyTo(bytes, offset);
    }

    private static void WriteInt32(byte[] bytes, int offset, int value) => BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(offset, 4), value);
    private static void WriteUInt32(byte[] bytes, int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset, 4), value);
    private static void WriteUInt16(byte[] bytes, int offset, ushort value) => BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(offset, 2), value);
    private static void WriteFloat(byte[] bytes, int offset, float value) => WriteInt32(bytes, offset, BitConverter.SingleToInt32Bits(value));
    private static void WriteVector2(byte[] bytes, int offset, Vector2 value)
    {
        WriteFloat(bytes, offset, value.X);
        WriteFloat(bytes, offset + 4, value.Y);
    }
    private static void WriteVector3(byte[] bytes, int offset, Vector3 value)
    {
        WriteFloat(bytes, offset, value.X);
        WriteFloat(bytes, offset + 4, value.Y);
        WriteFloat(bytes, offset + 8, value.Z);
    }
}
