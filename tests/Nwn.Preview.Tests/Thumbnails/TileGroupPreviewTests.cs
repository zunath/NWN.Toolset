using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Preview.Scene;
using Nwn.Preview.Thumbnails;
using System.Numerics;

namespace Nwn.Preview.Tests.Thumbnails;

[TestClass]
public sealed class TileGroupPreviewTests
{
    [TestMethod]
    public void ComposesRowMajorSlotsCenteredOnMultiCellFootprintAndCopiesHostMetadata()
    {
        var metadataCopies = new List<(RenderMesh Source, RenderMesh Target)>();
        var first = Model("first");
        var second = Model("second");
        var third = Model("third");

        var composed = TileGroupPreview.Compose(
            [first, null, second, third],
            columns: 2,
            rows: 2,
            copyMeshMetadata: (source, target) => metadataCopies.Add((source, target)));

        Assert.IsNotNull(composed);
        Assert.AreEqual(3, composed.Meshes.Count);
        CollectionAssert.AreEqual(new[] { "first", "second", "third" }, composed.Meshes.Select(mesh => mesh.NodeName).ToArray());
        Assert.AreSame(first.Meshes[0].Positions, composed.Meshes[0].Positions);
        Assert.AreSame(first.Meshes[0].Indices, composed.Meshes[0].Indices);
        Assert.AreEqual(new Vector3(-5f, -5f, 0f), Vector3.Transform(Vector3.Zero, composed.Meshes[0].Transform));
        Assert.AreEqual(new Vector3(-5f, 5f, 0f), Vector3.Transform(Vector3.Zero, composed.Meshes[1].Transform));
        Assert.AreEqual(new Vector3(5f, 5f, 0f), Vector3.Transform(Vector3.Zero, composed.Meshes[2].Transform));
        Assert.AreEqual(3, metadataCopies.Count);
        Assert.AreSame(first.Meshes[0], metadataCopies[0].Source);
        Assert.AreSame(composed.Meshes[0], metadataCopies[0].Target);
    }

    private static RenderModel Model(string name)
    {
        var mesh = new RenderMesh
        {
            NodeName = name,
            TextureName = string.Empty,
            Positions = [0f, 0f, 0f],
            Normals = [],
            TexCoords = [],
            Indices = [],
            Transform = Matrix4x4.Identity
        };
        return new RenderModel { Name = name, Meshes = [mesh] };
    }
}
