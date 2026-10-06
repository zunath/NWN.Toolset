using System.Numerics;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Formats.Mdl;
using Nwn.Preview.Scene;

namespace Nwn.Preview.Tests.Scene;

[TestClass]
public sealed class MdlScenePreparerTests
{
    [TestMethod]
    public void Prepare_ResolvesNestedUniformScaleAxisAngleAndNormalsWithoutApplyingAnimations()
    {
        var model = MdlAsciiReader.Read(Encoding.ASCII.GetBytes(MdlSceneFixtures.StaticMeshModel(withAnimation: true)));

        var prepared = MdlScenePreparer.Prepare(model);

        Assert.IsTrue(prepared.HasAnimations);
        Assert.IsFalse(prepared.AnimationTracksApplied);
        Assert.AreEqual("root", prepared.Nodes[1].ParentName);
        AssertVector(new Vector3(1, 4, 0), prepared.Nodes[1].Mesh!.Vertices[0]);
        AssertVector(new Vector3(-1, 2, 0), prepared.Nodes[1].Mesh!.Vertices[1]);
        AssertVector(new Vector3(0, 1, 0), prepared.Nodes[1].Mesh!.Normals[0]);
        Assert.AreEqual("armor_diffuse", prepared.Nodes[1].Mesh!.BitmapName);
    }

    [TestMethod]
    public void Prepare_ComputesExactModelSpaceBoundsAndLeavesVertexlessScenesUnbounded()
    {
        var source = MdlAsciiReader.Read(Encoding.ASCII.GetBytes(MdlSceneFixtures.StaticMeshModel(withAnimation: false)));

        var prepared = MdlScenePreparer.Prepare(source);

        Assert.IsNotNull(prepared.Bounds);
        var bounds = prepared.Bounds.Value;
        AssertVector(new Vector3(-1, 2, 0), bounds.Minimum);
        AssertVector(new Vector3(1, 4, 2), bounds.Maximum);
        var midpoint = (bounds.Minimum + bounds.Maximum) * 0.5f;
        var halfDiagonal = Math.Max(0.01f, Vector3.Distance(bounds.Minimum, bounds.Maximum) * 0.5f);
        AssertVector(new Vector3(0, 3, 1), midpoint);
        Assert.AreEqual(MathF.Sqrt(3), halfDiagonal, 0.00001f);
        AssertVector(midpoint, bounds.Center);
        Assert.AreEqual(halfDiagonal, bounds.Radius, 0.00001f);

        var vertexless = MdlAsciiReader.Read(Encoding.ASCII.GetBytes(
            "newmodel empty" + Environment.NewLine +
            "beginmodelgeom empty" + Environment.NewLine +
            "node dummy root" + Environment.NewLine +
            "parent NULL" + Environment.NewLine +
            "endnode" + Environment.NewLine +
            "endmodelgeom" + Environment.NewLine));
        var emptyPrepared = MdlScenePreparer.Prepare(vertexless);

        Assert.IsNull(emptyPrepared.Bounds);
    }
    [TestMethod]
    public void Prepare_RejectsNonzeroAxisAngleWithNoAxisAndDegenerateScale()
    {
        var noAxisText = MdlSceneFixtures.StaticMeshModel(withAnimation: false).Replace("orientation 0 0 1 1.5707963", "orientation 0 0 0 1");
        Assert.ThrowsExactly<FormatException>(() => MdlScenePreparer.Prepare(
            MdlAsciiReader.Read(Encoding.ASCII.GetBytes(noAxisText))));
        var zeroScaleText = MdlSceneFixtures.StaticMeshModel(withAnimation: false).Replace("scale 2", "scale 0");
        Assert.ThrowsExactly<FormatException>(() => MdlScenePreparer.Prepare(
            MdlAsciiReader.Read(Encoding.ASCII.GetBytes(zeroScaleText))));
    }

    [TestMethod]
    public void Prepare_RejectsMissingParentsAndExcessiveHierarchyDepth()
    {
        var text = new StringBuilder("newmodel deep\nbeginmodelgeom deep\n");
        for (var i = 129; i >= 0; i--)
            text.Append($"node dummy n{i}\nparent {(i == 0 ? "NULL" : $"n{i - 1}")}\nendnode\n");
        text.Append("endmodelgeom\n");
        var scene = MdlAsciiReader.Read(Encoding.ASCII.GetBytes(text.ToString()));
        Assert.ThrowsExactly<FormatException>(() => MdlScenePreparer.Prepare(scene));
    }

    private static void AssertVector(Vector3 expected, Vector3 actual)
    {
        Assert.AreEqual(expected.X, actual.X, 0.00001f);
        Assert.AreEqual(expected.Y, actual.Y, 0.00001f);
        Assert.AreEqual(expected.Z, actual.Z, 0.00001f);
    }
}
