using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Preview.Areas;

namespace Nwn.Preview.Tests.Areas;

[TestClass]
public sealed class AreaGeometryTests
{
    [TestMethod]
    public void RaycastGround_TransformsTileLocalWalkmeshAndPrefersWalkableFaces()
    {
        var tile = new TilePlacement
        {
            TileIndex = 0,
            Column = 0,
            Row = 0,
            TileId = 0,
            Orientation = 0,
            HeightLevel = 0,
            CenterX = 0,
            CenterY = 0,
            HeightOffset = 4,
            Transform = Matrix4x4.CreateTranslation(0, 0, 4),
            IsFallback = false,
            Walkmesh = new WalkMesh
            {
                Vertices = new[]
                {
                    new Vector3(-2, -2, 0), new Vector3(2, -2, 0), new Vector3(0, 2, 0),
                    new Vector3(-2, -2, 1), new Vector3(2, -2, 1), new Vector3(0, 2, 1)
                },
                Faces = new[]
                {
                    new WalkFace { A = 0, B = 1, C = 2, Material = 0, Walkable = true },
                    new WalkFace { A = 3, B = 4, C = 5, Material = 1, Walkable = false }
                }
            }
        };
        var scene = new AreaScene
        {
            Tileset = "fixture",
            Width = 1,
            Height = 1,
            Tiles = new[] { tile },
            Instances = Array.Empty<InstanceMarker>(),
            Diagnostics = new AreaSceneDiagnostics()
        };

        var ray = new PickRay(new Vector3(0, 0, 10), new Vector3(0, 0, -1));

        Assert.AreEqual(4f, AreaWalkmesh.RaycastGround(ray, scene)!.Value.Z, 0.0001f);
        Assert.AreEqual(5f, AreaWalkmesh.RaycastGround(ray, scene, preferWalkable: false)!.Value.Z, 0.0001f);
    }

    [TestMethod]
    public void CameraFraming_UsesWorldSpaceBoundsForSingleInstance()
    {
        var model = new Nwn.Preview.Scene.RenderModel
        {
            Meshes = new[]
            {
                new Nwn.Preview.Scene.RenderMesh
                {
                    NodeName = "mesh",
                    TextureName = "",
                    Positions = new[] { -1f, -1f, 0f, 1f, 1f, 2f },
                    Normals = Array.Empty<float>(),
                    TexCoords = Array.Empty<float>(),
                    Indices = new[] { 0, 1, 0 },
                    Transform = Matrix4x4.Identity
                }
            }
        };
        var scene = new AreaScene
        {
            Tileset = "fixture",
            Width = 1,
            Height = 1,
            Tiles = Array.Empty<TilePlacement>(),
            Instances = new[]
            {
                new InstanceMarker
                {
                    Kind = InstanceMarkerKind.Placeable,
                    Position = new Vector3(15, 8, 3),
                    Orientation = Vector2.UnitX,
                    VisualTransform = Matrix4x4.Identity,
                    Model = model
                }
            },
            Diagnostics = new AreaSceneDiagnostics()
        };

        var framing = AreaCameraMath.ComputeSceneFraming(scene, AreaGrid.TileSize, MathF.PI / 3, 1.5f);

        Assert.AreEqual(15f, framing.Target.X, 0.0001f);
        Assert.AreEqual(8f, framing.Target.Y, 0.0001f);
        Assert.AreEqual(4f, framing.Target.Z, 0.0001f);
        Assert.AreEqual(3.3f, framing.Distance, 0.01f);
    }
}
