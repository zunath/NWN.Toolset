using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Areas.Editing;
using Nwn.Authoring.Editing;
using Nwn.Authoring.Resources;
using Nwn.Formats.Tilesets;
using Nwn.Preview.Areas;
using Nwn.Preview.Scene;

namespace Nwn.Preview.Tests.Areas;

[TestClass]
public sealed class AreaSceneComposerTests
{
    [TestMethod]
    public void Build_ComposesTilesAndNativeInstancesThroughHostResolvers()
    {
        var tile = new JsonGffStruct();
        tile.SetInt("Tile_ID", GffFieldType.Int, 0);
        tile.SetInt("Tile_Orientation", GffFieldType.Int, 1);
        tile.SetInt("Tile_Height", GffFieldType.Int, 0);
        var areaRoot = new JsonGffStruct();
        areaRoot.SetString("Tileset", GffFieldType.ResRef, "fixture");
        areaRoot.SetInt("Width", GffFieldType.Int, 1);
        areaRoot.SetInt("Height", GffFieldType.Int, 1);
        areaRoot.Add("Tile_List", CreateList(tile));
        var are = new AreDocument(new JsonGffDocument("ARE ", areaRoot));

        var point = new JsonGffStruct();
        point.SetSingle("PointX", 0f);
        point.SetSingle("PointY", 0f);
        point.SetSingle("PointZ", 3f);
        var geometry = CreateList(point);
        var trigger = new JsonGffStruct();
        trigger.SetSingle("XPosition", 5f);
        trigger.SetSingle("YPosition", 5f);
        trigger.SetSingle("ZPosition", 2f);
        trigger.SetSingle("XOrientation", 0f);
        trigger.SetSingle("YOrientation", 1f);
        trigger.Add("Geometry", geometry);
        var gitRoot = new JsonGffStruct();
        gitRoot.Add("TriggerList", CreateList(trigger));
        var git = new GitDocument(new JsonGffDocument("GIT ", gitRoot));

        var model = new RenderModel { Meshes = Array.Empty<RenderMesh>() };
        var correction = Matrix4x4.CreateScale(2f);
        var callbackTypes = new List<ModuleResourceType>();
        var walkmesh = new WalkMesh
        {
            Vertices = new[] { new Vector3(-10, -10, 1), new Vector3(10, -10, 1), new Vector3(0, 10, 1) },
            Faces = new[] { new WalkFace { A = 0, B = 1, C = 2, Material = 0, Walkable = true } }
        };
        var resolvers = new AreaSceneResolvers
        {
            ResolveTileModel = name => name == "tile" ? model : null,
            ResolveTileWalkmesh = _ => walkmesh,
            ResolveInstanceAppearance = (type, _) =>
            {
                callbackTypes.Add(type);
                return new ResolvedInstanceAppearance
                {
                    Model = model,
                    ModelCorrection = correction,
                    IsDoorTransition = true,
                    TintMapOverrides = new Dictionary<string, int> { ["skin"] = 4 }
                };
            }
        };
        var tileset = new TilesetDefinition
        {
            Interior = true,
            Transition = 10,
            Tiles = new[]
            {
                new TileDefinition { Model = "tile", Doors = new[] { new TileDoorDefinition(2, 1, 2, 3, 90) } }
            }
        };

        var scene = AreaSceneComposer.Build(are, git, tileset, resolvers);

        Assert.AreEqual(1, scene.Tiles.Count);
        Assert.AreSame(model, scene.Tiles[0].Model);
        Assert.AreEqual(5f, scene.Tiles[0].CenterX, 0.0001f);
        Assert.IsFalse(scene.Tiles[0].IsFallback);
        Assert.AreEqual(1, scene.Instances.Count);
        var marker = scene.Instances[0];
        Assert.AreEqual(InstanceMarkerKind.Trigger, marker.Kind);
        Assert.AreEqual(new Vector3(5, 5, 2), marker.Position);
        Assert.AreEqual(1f, marker.Geometry![0].Z, 0.0001f,
            "trigger vertices are translated and draped to the resolved tile walkmesh");
        Assert.AreEqual(correction, marker.VisualTransform);
        Assert.IsTrue(marker.IsDoorTransition);
        Assert.AreEqual(4, marker.TintMapOverrides["skin"]);
        CollectionAssert.AreEqual(new[] { ModuleResourceType.Utt }, callbackTypes);
        Assert.AreEqual(1, scene.DoorAnchors.Count);
        Assert.IsTrue(scene.IsInteriorTileset);
        Assert.AreEqual(0, marker.ListIndex);
    }

    [TestMethod]
    public void IncrementalInstanceMarkerRetainsTheFullComposersKindLocalListOrdinal()
    {
        var first = CreatePlaceable("fixture_place_a", 1f, 2f);
        var second = CreatePlaceable("fixture_place_b", 7f, 8f);
        var gitRoot = new JsonGffStruct();
        gitRoot.Add("Placeable List", CreateList(first, second));
        var are = new AreDocument(new JsonGffDocument("ARE ", new JsonGffStruct()));
        var git = new GitDocument(new JsonGffDocument("GIT ", gitRoot));
        var resolvers = new AreaSceneResolvers { ResolveTileModel = _ => null };

        var fullScene = AreaSceneComposer.Build(are, git, null, resolvers);
        var fullMarker = fullScene.Instances[1];
        var incrementalMarker = AreaSceneComposer.BuildInstanceMarker(
            ModuleResourceType.Utp, second, resolvers, listIndex: 1);
        var standaloneMarker = AreaSceneComposer.BuildInstanceMarker(
            ModuleResourceType.Utp, second, resolvers);

        Assert.AreEqual(1, fullMarker.ListIndex);
        Assert.AreEqual(fullMarker.ListIndex, incrementalMarker.ListIndex);
        Assert.AreEqual(fullMarker.Kind, incrementalMarker.Kind);
        Assert.AreEqual(fullMarker.Position, incrementalMarker.Position);
        Assert.AreEqual(fullMarker.Orientation, incrementalMarker.Orientation);
        Assert.AreEqual(fullMarker.TemplateResRef, incrementalMarker.TemplateResRef);
        Assert.AreEqual(-1, standaloneMarker.ListIndex,
            "standalone preview markers remain outside any GIT list");
    }

    [TestMethod]
    public void SceneInstanceEditorUsesListOrdinalForMoveAndUpdatesSceneInPlace()
    {
        var instance = new JsonGffStruct();
        instance.SetSingle("X", 1f);
        instance.SetSingle("Y", 2f);
        instance.SetSingle("Z", 3f);
        instance.SetSingle("Bearing", 0f);
        var gitRoot = new JsonGffStruct();
        gitRoot.Add("Placeable List", CreateList(instance));
        var areaRoot = new JsonGffStruct();
        var commentsRoot = new JsonGffStruct();
        var gitSession = new DocumentSession("area.git", new JsonGffDocument("GIT ", gitRoot));
        var documents = new AreaDocumentEditSession(
            new DocumentSession("area.are", new JsonGffDocument("ARE ", areaRoot)),
            gitSession,
            new DocumentSession("area.gic", new JsonGffDocument("GIC ", commentsRoot)));
        using (documents)
        {
            var marker = new InstanceMarker
            {
                Kind = InstanceMarkerKind.Placeable,
                ListIndex = 0,
                Position = new Vector3(1, 2, 3),
                Orientation = Vector2.UnitX
            };
            var scene = new AreaScene
            {
                Tileset = "fixture",
                Width = 1,
                Height = 1,
                Tiles = Array.Empty<TilePlacement>(),
                Instances = new[] { marker },
                Diagnostics = new AreaSceneDiagnostics()
            };
            var editor = new AreaSceneInstanceEditor(new AreaInstanceEditor(documents));

            var result = editor.Move(scene, marker, new Vector3(9, 8, 7));

            Assert.AreEqual(AreaSceneInstanceEditOutcome.Changed, result.Outcome);
            Assert.AreEqual(new Vector3(9, 8, 7), result.UpdatedMarker!.Position);
            Assert.AreEqual(new Vector3(9, 8, 7), result.Scene!.Instances[0].Position);
            Assert.AreEqual(9f, documents.Instances.Document.Root.Get("Placeable List").Elements![0].GetSingleOrNull("X"));
            Assert.IsTrue(documents.UndoLatest());
            Assert.AreEqual(1f, documents.Instances.Document.Root.Get("Placeable List").Elements![0].GetSingleOrNull("X"));
        }
    }

    private static JsonGffStruct CreatePlaceable(string resRef, float x, float y)
    {
        var instance = new JsonGffStruct();
        instance.SetString("TemplateResRef", GffFieldType.ResRef, resRef);
        instance.SetSingle("X", x);
        instance.SetSingle("Y", y);
        instance.SetSingle("Z", 0f);
        instance.SetSingle("Bearing", 0f);
        return instance;
    }

    private static JsonGffField CreateList(params JsonGffStruct[] entries)
    {
        var field = JsonGffField.CreateList();
        foreach (var entry in entries)
            field.InsertElement(field.Elements!.Count, entry);
        return field;
    }
}
