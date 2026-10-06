using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Formats.NativeModels;
using Nwn.Preview.Scene;

namespace Nwn.Preview.Tests.Scene;

[TestClass]
public sealed class MdlMeshBuilderTests
{
    [TestMethod]
    public void Build_ExposesTheBuiltSourceMeshAndSupportsHostSelectionPolicy()
    {
        var visible = Triangle("visible");
        var hidden = Triangle("hidden");
        visible.Children.Add(hidden);
        hidden.Parent = visible;
        var model = new MdlModel { Name = "policy" , GeometryRoot = visible };
        MdlTrimeshNode? callbackSource = null;
        RenderMesh? callbackResult = null;

        var result = MdlMeshBuilder.Build(model, new MdlMeshBuildOptions
        {
            IncludeRenderedMesh = mesh => mesh.Name == "visible",
            MeshBuilt = (source, built) =>
            {
                callbackSource = source;
                callbackResult = built;
            }
        });

        Assert.AreEqual(1, result.Meshes.Count);
        Assert.AreSame(visible, callbackSource);
        Assert.AreSame(result.Meshes[0], callbackResult);
        Assert.AreEqual("visible", result.Meshes[0].NodeName);
    }

    [TestMethod]
    public void PlaceablePreviewOptionsRetainTheHiddenSelectionVolume()
    {
        var selectionVolume = Triangle("selection_volume");
        selectionVolume.Render = false;
        var model = new MdlModel { Name = "invisible_placeable", GeometryRoot = selectionVolume };

        var convenience = MdlMeshBuilder.BuildPlaceablePreview(model);
        var configured = MdlMeshBuilder.Build(model, new MdlMeshBuildOptions
        {
            Purpose = MdlMeshBuildPurpose.PlaceablePreview
        });

        Assert.IsTrue(convenience.IsInvisiblePlaceableGeometry);
        Assert.AreEqual(convenience.IsInvisiblePlaceableGeometry, configured.IsInvisiblePlaceableGeometry);
        CollectionAssert.AreEqual(
            convenience.Meshes.Select(mesh => mesh.NodeName).ToArray(),
            configured.Meshes.Select(mesh => mesh.NodeName).ToArray());
    }

    [TestMethod]
    public void AnimatedPreviewOptionsKeepOnlyTheSettledPoseWhenThereAreNoNamedClips()
    {
        var mesh = Triangle("animated");
        var model = new MdlModel { Name = "pose_reduction", GeometryRoot = mesh };
        IReadOnlyList<IReadOnlyDictionary<string, PosedNode>> poses =
        [
            new Dictionary<string, PosedNode>(StringComparer.OrdinalIgnoreCase)
            {
                [mesh.Name] = new(new Vector3(1, 0, 0), Quaternion.Identity, 1f)
            },
            new Dictionary<string, PosedNode>(StringComparer.OrdinalIgnoreCase)
            {
                [mesh.Name] = new(new Vector3(2, 0, 0), Quaternion.Identity, 1f)
            }
        ];

        var convenience = MdlMeshBuilder.BuildAnimatedPreview(model, poses, []);
        var configured = MdlMeshBuilder.Build(model, new MdlMeshBuildOptions
        {
            Purpose = MdlMeshBuildPurpose.AnimatedPreview,
            PoseFrames = poses,
            Animations = []
        });

        CollectionAssert.AreEqual(
            convenience.Meshes[0].PoseFrames.ToArray(),
            configured.Meshes[0].PoseFrames.ToArray());
        Assert.AreEqual(1, configured.Meshes[0].PoseFrames.Count);
        Assert.AreEqual(2f, configured.Meshes[0].Transform.M41);
    }

    private static MdlTrimeshNode Triangle(string name) => new()
    {
        Name = name,
        Vertices = [Vector3.Zero, Vector3.UnitX, Vector3.UnitY],
        Faces = [new MdlFace { VertexIndex0 = 0, VertexIndex1 = 1, VertexIndex2 = 2 }]
    };
}
