using System.Numerics;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Formats.Mdl;

namespace Nwn.Formats.Tests.Mdl;

[TestClass]
public sealed class MdlAsciiReaderTests
{
    [TestMethod]
    public void StaticAsciiModel_PreservesHierarchyTransformsMeshMaterialAndAnimationPresence()
    {
        var scene = MdlAsciiReader.Read(Encoding.ASCII.GetBytes(MdlFixtures.StaticMeshModel(withAnimation: true)));

        Assert.AreEqual("part", scene.ModelName);
        Assert.IsTrue(scene.HasAnimations);
        Assert.AreEqual(2, scene.Nodes.Count);
        var meshNode = scene.Nodes[1];
        Assert.AreEqual(MdlNodeType.Trimesh, meshNode.Type);
        Assert.AreEqual("root", meshNode.ParentName);
        Assert.AreEqual(new Vector3(0, 2, 0), meshNode.Position);
        Assert.AreEqual(new Vector4(0, 0, 1, 1.5707963f), meshNode.Orientation);
        Assert.AreEqual(2, meshNode.Scale);
        Assert.AreEqual("armor_diffuse", meshNode.Mesh!.BitmapName);
        Assert.AreEqual("armor_surface", meshNode.Mesh.MaterialName);
        Assert.AreEqual(new Vector3(1, 0, 0), meshNode.Mesh.Normals[0]);
        Assert.AreEqual(new Vector2(1, 1), meshNode.Mesh.TextureVertices[2]);
        Assert.AreEqual(new MdlTriangle(0, 1, 2, 7, 0, 1, 2, 3), meshNode.Mesh.Faces[0]);
    }

    [TestMethod]
    public void StaticAsciiModel_AllowsMissingNormalsAndOptionalTvertsForUntexturedNodes()
    {
        var text = MdlFixtures.StaticMeshModel(withAnimation: false)
            .Replace("  normals 3\n    1 0 0\n    1 0 0\n    1 0 0\n", string.Empty)
            .Replace("  tverts 3\n    0 0 0\n    1 0 0\n    1 1 0\n", string.Empty);
        var scene = MdlAsciiReader.Read(Encoding.ASCII.GetBytes(text));

        Assert.AreEqual(0, scene.Nodes[1].Mesh!.Normals.Count);
        Assert.AreEqual(0, scene.Nodes[1].Mesh!.TextureVertices.Count);
    }

    [TestMethod]
    public void Reader_RejectsMalformedIndicesHierarchyCountsAndUnsupportedNodes()
    {
        var badIndex = MdlFixtures.StaticMeshModel(withAnimation: false).Replace("0 1 2 7 0 1 2 3", "0 1 9 7 0 1 2 3");
        Assert.ThrowsExactly<FormatException>(() => MdlAsciiReader.Read(Encoding.ASCII.GetBytes(badIndex)));
        var missingParent = MdlFixtures.StaticMeshModel(withAnimation: false).Replace("parent root", "parent missing");
        Assert.ThrowsExactly<FormatException>(() => MdlAsciiReader.Read(Encoding.ASCII.GetBytes(missingParent)));
        var skin = MdlFixtures.StaticMeshModel(withAnimation: false).Replace("node trimesh plate", "node skin plate");
        Assert.ThrowsExactly<NotSupportedException>(() => MdlAsciiReader.Read(Encoding.ASCII.GetBytes(skin)));
        Assert.ThrowsExactly<FormatException>(() => MdlAsciiReader.Read(Encoding.ASCII.GetBytes(MdlFixtures.StaticMeshModel(false)),
            new MdlAsciiReadOptions { MaximumInputBytes = 10 }));
    }
}
