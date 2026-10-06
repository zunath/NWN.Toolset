using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Formats.NativeModels;
using Nwn.Preview.Scene;

namespace Nwn.Preview.Tests.Scene;

[TestClass]
public sealed class MdlGeometryFlattenerTests
{
    [TestMethod]
    public void FlattenNodeTransformsBakesChildScaleAndParentTranslationIntoGeometry()
    {
        var mesh = new MdlTrimeshNode
        {
            Name = "mesh",
            Vertices = [Vector3.UnitX],
            Normals = [Vector3.UnitZ]
        };
        var child = new MdlNode { Name = "child", Scale = 2f };
        var root = new MdlNode { Name = "root", Position = new Vector3(2f, 0f, 0f) };
        root.Children.Add(child);
        child.Parent = root;
        child.Children.Add(mesh);
        mesh.Parent = child;
        var model = new MdlModel { GeometryRoot = root };

        MdlGeometryFlattener.FlattenNodeTransforms(model);

        Assert.AreEqual(new Vector3(4f, 0f, 0f), mesh.Vertices[0]);
        Assert.AreEqual(Vector3.UnitZ, mesh.Normals[0]);
        Assert.AreEqual(Vector3.Zero, root.Position);
        Assert.AreEqual(1f, child.Scale);
        Assert.AreEqual(new Vector3(4f, 0f, 0f), model.BoundsMaximum);
    }
}
