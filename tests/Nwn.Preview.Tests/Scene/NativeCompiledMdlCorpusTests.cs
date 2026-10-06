using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Numerics;
using Nwn.Formats.Mdl;
using Nwn.Preview.Scene;

namespace Nwn.Preview.Tests.Scene;

[TestClass]
public sealed class NativeCompiledMdlCorpusTests
{
    [TestMethod]
    public void XenomechFemaleSkeleton_PreparesBothImpactNodesAndRetainsDistinctAssemblyIdentities()
    {
        var root = Environment.GetEnvironmentVariable("XENOMECH_TEST_CONTENT_ROOT");
        Assert.IsFalse(string.IsNullOrWhiteSpace(root), "Select the read-only Xenomech corpus.");
        var source = MdlBinaryReader.Read(File.ReadAllBytes(Path.Combine(root!, "xm_pt_root", "pfh0.mdl")));
        var prepared = MdlScenePreparer.Prepare(source);
        var impacts = prepared.Nodes.Where(node => node.Name == "Impact").ToArray();
        Assert.AreEqual(2, impacts.Length);
        Assert.AreNotEqual(impacts[0].Id, impacts[1].Id);
        foreach (var impact in impacts)
        {
            var original = source.Nodes.Single(node => node.Id == impact.Id);
            var parent = prepared.Nodes.Single(node => node.Id == impact.ParentId);
            var expected = Matrix4x4.CreateTranslation(original.Position) * parent.WorldTransform;
            Assert.AreEqual(expected.Translation, impact.WorldTransform.Translation);
        }
        var assembled = SceneAssembler.Assemble("two-skeletons", [new("left", prepared, Matrix4x4.Identity),
            new("right", prepared, Matrix4x4.CreateTranslation(2, 0, 0))]);
        Assert.AreEqual(114, assembled.Nodes.Count);
        Assert.AreEqual(114, assembled.Nodes.Select(node => node.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.IsTrue(assembled.Nodes.Where(node => node.ParentId is not null)
            .All(node => assembled.Nodes.Any(parent => parent.Id == node.ParentId)));
    }

    [TestMethod]
    public void SwlorCompiledChest_PreparesRigidMeshBoundsTransformsAndTexture()
    {
        var root = Environment.GetEnvironmentVariable("SWLOR_TEST_HAKS_ROOT");
        if (string.IsNullOrWhiteSpace(root))
            Assert.Inconclusive("Set SWLOR_TEST_HAKS_ROOT to run the read-only compiled armor check.");
        var path = Path.Combine(root!, "sw_pt_chest", "pfa0_chest001.mdl");
        if (!File.Exists(path))
            Assert.Inconclusive($"Native compiled chest fixture is unavailable: {path}");

        var source = MdlBinaryReader.Read(File.ReadAllBytes(path));
        var prepared = MdlScenePreparer.Prepare(source);

        Assert.AreEqual("pfa0_chest001", source.ModelName);
        Assert.AreEqual(2, source.Nodes.Count);
        Assert.IsFalse(source.HasAnimations, "Single-key node controllers establish static transforms, not animated tracks.");
        Assert.IsFalse(prepared.AnimationTracksApplied);
        var sourceMeshNode = source.Nodes.Single(node => node.Mesh is not null);
        Assert.AreEqual(-0.000942781975f, sourceMeshNode.Position.X, 0.0000001f);
        Assert.AreEqual(Vector4.Zero, sourceMeshNode.Orientation);
        Assert.IsTrue(float.IsFinite(prepared.Nodes.Single(node => node.Mesh is not null).WorldTransform.M41));
        var mesh = prepared.Nodes.Single(node => node.Mesh is not null).Mesh!;
        Assert.IsTrue(mesh.Vertices.Count > 0);
        Assert.AreEqual(mesh.Vertices.Count, mesh.Normals.Count);
        Assert.AreEqual(mesh.Vertices.Count, mesh.TextureVertices.Count);
        Assert.IsTrue(mesh.Faces.Count > 0);
        Assert.IsNotNull(mesh.BitmapName);
        Assert.AreEqual(-0.000942929f, mesh.Vertices[0].X, 0.000001f);
        Assert.AreEqual(0.0603779f, mesh.Vertices[0].Y, 0.000001f);
        Assert.AreEqual(0.119153f, mesh.Vertices[0].Z, 0.000001f);
        Assert.IsTrue(mesh.Vertices.All(vertex => float.IsFinite(vertex.X) && float.IsFinite(vertex.Y) && float.IsFinite(vertex.Z)));
        Assert.IsTrue(mesh.Vertices.Any(vertex => vertex.Length() > 0.1f));
        Assert.IsTrue(mesh.Faces.All(face => face.VertexA < mesh.Vertices.Count && face.VertexB < mesh.Vertices.Count && face.VertexC < mesh.Vertices.Count));
    }
}
