using System.Numerics;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Formats.Mdl;
using Nwn.Preview.Scene;

namespace Nwn.Preview.Tests.Scene;

[TestClass]
public sealed class SceneAssemblerTests
{
    [TestMethod]
    public void SeparateInstancesRetainTheirHierarchyAndIndependentMaterialKeysWithoutChangingTheSource()
    {
        var source = Prepare();
        var transform = Matrix4x4.CreateRotationZ(MathF.PI / 2) * Matrix4x4.CreateTranslation(3, 4, 5);
        var assembled = SceneAssembler.Assemble("body", [new("left", source, transform), new("right", source, Matrix4x4.Identity)]);
        var left = assembled.Nodes.Single(node => node.Name == "left/mesh");
        var right = assembled.Nodes.Single(node => node.Name == "right/mesh");
        Assert.AreEqual("left/root", left.ParentName);
        Assert.AreEqual("left/paint", left.Mesh!.BitmapName);
        Assert.AreEqual("right/paint", right.Mesh!.BitmapName);
        Assert.AreEqual(SceneAssembler.ScopedName("left", "paint"), left.Mesh.BitmapName);
        AssertVector(new(1, 6, 5), left.Mesh.Vertices[1]);
        AssertVector(new(1, 2, 0), right.Mesh.Vertices[0]);
        AssertVector(new(1, 2, 0), source.Nodes[1].Mesh!.Vertices[0]);
        Assert.AreNotSame(source.Nodes[1].Mesh, right.Mesh);
        Assert.IsNotNull(assembled.Bounds);
        AssertVector(new(0, 2, 0), assembled.Bounds.Value.Minimum);
        // Left vertices rotate to (1,5,5), (1,6,5), (0,5,5); right stays (1,2,0), (2,2,0), (1,3,0).
        AssertVector(new(2, 6, 5), assembled.Bounds.Value.Maximum);
        Assert.IsFalse(assembled.AnimationTracksApplied);
    }

    [TestMethod]
    public void NonuniformScaleUsesInverseTransposeNormalsAndReflectionReversesBothFaceIndexSets()
    {
        var source = Prepare();
        var scaled = SceneAssembler.Assemble("scale", [new("part", source, Matrix4x4.CreateScale(2, 1, 3))]);
        AssertVector(Vector3.Normalize(new(0.5f, 1, 0)), scaled.Nodes[1].Mesh!.Normals[0]);
        var mirrored = SceneAssembler.Assemble("mirror", [new("part", source, Matrix4x4.CreateScale(-1, 1, 1))]);
        var face = mirrored.Nodes[1].Mesh!.Faces[0];
        Assert.AreEqual((0, 2, 1, 0, 2, 1), (face.VertexA, face.VertexB, face.VertexC, face.TextureA, face.TextureB, face.TextureC));
        Assert.AreEqual((0, 1, 2), (source.Nodes[1].Mesh!.Faces[0].VertexA, source.Nodes[1].Mesh!.Faces[0].VertexB, source.Nodes[1].Mesh!.Faces[0].VertexC));
    }

    [TestMethod]
    public void RejectsAmbiguousIdentitiesSingularProjectiveAndNonfiniteTransforms()
    {
        var scene = Prepare();
        Assert.ThrowsExactly<ArgumentException>(() => SceneAssembler.Assemble("duplicate", [new("part", scene, Matrix4x4.Identity), new("PART", scene, Matrix4x4.Identity)]));
        Assert.ThrowsExactly<ArgumentException>(() => SceneAssembler.Assemble("scope", [new("part/root", scene, Matrix4x4.Identity)]));
        Assert.ThrowsExactly<ArgumentException>(() => SceneAssembler.Assemble("scale", [new("part", scene, Matrix4x4.CreateScale(0))]));
        Assert.ThrowsExactly<ArgumentException>(() => SceneAssembler.Assemble("projective", [new("part", scene, Matrix4x4.CreatePerspectiveFieldOfView(1, 1, 0.1f, 100))]));
        Assert.ThrowsExactly<ArgumentException>(() => SceneAssembler.Assemble("infinite", [new("part", scene, Matrix4x4.CreateTranslation(float.PositiveInfinity, 0, 0))]));
    }

    private static PreparedScene Prepare() => MdlScenePreparer.Prepare(MdlAsciiReader.Read(Encoding.ASCII.GetBytes("""
        newmodel part
        beginmodelgeom part
        node dummy root
        parent NULL
        position 1 2 0
        endnode
        node trimesh mesh
        parent root
        bitmap paint
        verts 3
        0 0 0
        1 0 0
        0 1 0
        normals 3
        1 1 0
        1 1 0
        1 1 0
        tverts 3
        0 0 0
        1 0 0
        0 1 0
        faces 1
        0 1 2 1 0 1 2 0
        endnode
        endmodelgeom
        """)));

    private static void AssertVector(Vector3 expected, Vector3 actual)
    {
        Assert.AreEqual(expected.X, actual.X, 0.00001f);
        Assert.AreEqual(expected.Y, actual.Y, 0.00001f);
        Assert.AreEqual(expected.Z, actual.Z, 0.00001f);
    }
}
