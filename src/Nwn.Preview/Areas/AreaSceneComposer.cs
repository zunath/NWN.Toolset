// SPDX-License-Identifier: MIT

using System.Numerics;
using Nwn.Authoring.Areas.Placement;
using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Resources;
using Nwn.Formats.Tilesets;
using Nwn.Preview.Scene;

namespace Nwn.Preview.Areas;

/// <summary>Composes area tile and instance documents into a render-ready neutral scene.</summary>
public static class AreaSceneComposer
{
    public const float TileSize = AreaGrid.TileSize;

    public static AreaScene Build(AreDocument are, GitDocument git, TilesetDefinition? tileset,
        AreaSceneResolvers resolvers)
    {
        ArgumentNullException.ThrowIfNull(are);
        ArgumentNullException.ThrowIfNull(git);
        ArgumentNullException.ThrowIfNull(resolvers);
        ArgumentNullException.ThrowIfNull(resolvers.ResolveTileModel);

        var diagnostics = new AreaSceneDiagnostics();
        var tilesetResRef = are.Tileset ?? string.Empty;
        var width = are.Width ?? 0;
        var height = are.Height ?? 0;
        if (tileset == null)
            diagnostics.AddMissingModel($"tileset '{tilesetResRef}' could not be resolved/parsed; every tile in this area falls back.");

        var tiles = BuildTiles(are, tileset, tilesetResRef, width, resolvers, diagnostics);
        var instances = BuildInstances(git, resolvers, tiles);
        return new AreaScene
        {
            Tileset = tilesetResRef,
            Width = width,
            Height = height,
            Tiles = tiles,
            Instances = instances,
            DoorAnchors = BuildDoorAnchors(tiles, tileset),
            IsInteriorTileset = tileset?.Interior ?? false,
            Diagnostics = diagnostics,
            Lighting = ComputeLighting(are)
        };
    }

    private static AreaLighting ComputeLighting(AreDocument are)
    {
        var isNight = are.IsNight ?? false;
        var ambient = isNight ? are.MoonAmbientColor : are.SunAmbientColor;
        var diffuse = isNight ? are.MoonDiffuseColor : are.SunDiffuseColor;
        var fog = isNight ? are.MoonFogColor : are.SunFogColor;
        var fogAmount = isNight ? are.MoonFogAmount : are.SunFogAmount;
        return new AreaLighting
        {
            AmbientColor = ambient is { } a ? AreaLighting.DecodeColor(a) : AreaLighting.Default.AmbientColor,
            DiffuseColor = diffuse is { } d ? AreaLighting.DecodeColor(d) : AreaLighting.Default.DiffuseColor,
            IsNight = isNight,
            FogColor = fog is { } f ? AreaLighting.DecodeColor(f) : Vector3.Zero,
            FogDensity = AreaLighting.DecodeFogDensity(fogAmount ?? 0)
        };
    }

    private static List<TilePlacement> BuildTiles(AreDocument are, TilesetDefinition? tileset,
        string tilesetResRef, int width, AreaSceneResolvers resolvers, AreaSceneDiagnostics diagnostics)
    {
        var tileStructs = are.Tiles;
        var placements = new List<TilePlacement>(tileStructs.Count);
        var effectiveWidth = width > 0 ? width : 1;
        for (var i = 0; i < tileStructs.Count; i++)
        {
            var tileStruct = tileStructs[i];
            var tileId = tileStruct.GetIntOrNull("Tile_ID") ?? -1;
            var orientation = tileStruct.GetIntOrNull("Tile_Orientation") ?? 0;
            var tileHeight = tileStruct.GetIntOrNull("Tile_Height") ?? 0;
            var col = i % effectiveWidth;
            var row = i / effectiveWidth;
            var centerX = col * TileSize + TileSize / 2f;
            var centerY = row * TileSize + TileSize / 2f;
            var heightOffset = tileset != null ? tileHeight * tileset.Transition : 0;
            var angle = (float)(orientation * (Math.PI / 2.0));
            var transform = Matrix4x4.CreateRotationZ(angle) * Matrix4x4.CreateTranslation(centerX, centerY, heightOffset);
            string? modelResRef = null;
            RenderModel? model = null;
            var isFallback = false;

            if (tileset == null)
                isFallback = true;
            else if (tileId < 0 || tileId >= tileset.Tiles.Count)
            {
                isFallback = true;
                diagnostics.AddMissingModel($"tile #{i} (col {col}, row {row}): Tile_ID {tileId} is out of range for tileset '{tilesetResRef}' ({tileset.Tiles.Count} tiles).");
            }
            else
            {
                modelResRef = tileset.Tiles[tileId].Model;
                if (string.IsNullOrWhiteSpace(modelResRef))
                {
                    isFallback = true;
                    diagnostics.AddMissingModel($"tile #{i} (col {col}, row {row}): Tile_ID {tileId} in tileset '{tilesetResRef}' has no Model ResRef.");
                }
                else
                {
                    model = resolvers.ResolveTileModel(modelResRef);
                    if (model == null)
                    {
                        isFallback = true;
                        diagnostics.AddMissingModel($"tile #{i} (col {col}, row {row}): model '{modelResRef}' (Tile_ID {tileId}, tileset '{tilesetResRef}') could not be resolved/parsed.");
                    }
                }
            }

            WalkMesh? walkmesh = null;
            if (resolvers.ResolveTileWalkmesh != null && !isFallback && modelResRef != null)
                walkmesh = resolvers.ResolveTileWalkmesh(modelResRef);
            placements.Add(new TilePlacement
            {
                TileIndex = i, Column = col, Row = row, TileId = tileId, Orientation = orientation,
                HeightLevel = tileHeight, CenterX = centerX, CenterY = centerY, HeightOffset = heightOffset,
                Transform = transform, ModelResRef = modelResRef, Model = model, IsFallback = isFallback,
                Walkmesh = walkmesh
            });
        }
        return placements;
    }

    private static List<InstanceMarker> BuildInstances(GitDocument git, AreaSceneResolvers resolvers,
        IReadOnlyList<TilePlacement> tiles)
    {
        var result = new List<InstanceMarker>();
        AddMarkers(result, git.Creatures, InstanceMarkerKind.Creature, ModuleResourceType.Utc, resolvers, tiles);
        AddMarkers(result, git.Doors, InstanceMarkerKind.Door, ModuleResourceType.Utd, resolvers, tiles);
        AddMarkers(result, git.Items, InstanceMarkerKind.Item, ModuleResourceType.Uti, resolvers, tiles);
        AddMarkers(result, git.Placeables, InstanceMarkerKind.Placeable, ModuleResourceType.Utp, resolvers, tiles);
        AddMarkers(result, git.Sounds, InstanceMarkerKind.Sound, ModuleResourceType.Uts, resolvers, tiles);
        AddMarkers(result, git.Stores, InstanceMarkerKind.Store, ModuleResourceType.Utm, resolvers, tiles);
        AddMarkers(result, git.Triggers, InstanceMarkerKind.Trigger, ModuleResourceType.Utt, resolvers, tiles, true);
        AddMarkers(result, git.Waypoints, InstanceMarkerKind.Waypoint, ModuleResourceType.Utw, resolvers, tiles);
        return result;
    }

    private static void AddMarkers(List<InstanceMarker> destination, IReadOnlyList<JsonGffStruct> instances,
        InstanceMarkerKind kind, ModuleResourceType type, AreaSceneResolvers resolvers,
        IReadOnlyList<TilePlacement>? tiles, bool includeGeometry = false)
    {
        for (var index = 0; index < instances.Count; index++)
            destination.Add(BuildMarker(instances[index], kind, type, resolvers, tiles, includeGeometry, index));
    }

    public static InstanceMarker BuildInstanceMarker(ModuleResourceType type, JsonGffStruct instance,
        AreaSceneResolvers resolvers, IReadOnlyList<TilePlacement>? tiles = null) =>
        BuildInstanceMarker(type, instance, resolvers, listIndex: -1, tiles: tiles);

    public static InstanceMarker BuildInstanceMarker(ModuleResourceType type, JsonGffStruct instance,
        AreaSceneResolvers resolvers, int listIndex, IReadOnlyList<TilePlacement>? tiles = null)
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(resolvers);
        var kind = type switch
        {
            ModuleResourceType.Utc => InstanceMarkerKind.Creature,
            ModuleResourceType.Utd => InstanceMarkerKind.Door,
            ModuleResourceType.Uti => InstanceMarkerKind.Item,
            ModuleResourceType.Utp => InstanceMarkerKind.Placeable,
            ModuleResourceType.Uts => InstanceMarkerKind.Sound,
            ModuleResourceType.Utm => InstanceMarkerKind.Store,
            ModuleResourceType.Utt => InstanceMarkerKind.Trigger,
            ModuleResourceType.Utw => InstanceMarkerKind.Waypoint,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "This resource type is not an area instance.")
        };
        return BuildMarker(instance, kind, type, resolvers, tiles, type == ModuleResourceType.Utt, listIndex);
    }

    private static InstanceMarker BuildMarker(JsonGffStruct instance, InstanceMarkerKind kind,
        ModuleResourceType type, AreaSceneResolvers resolvers, IReadOnlyList<TilePlacement>? tiles,
        bool includeGeometry,
        int listIndex)
    {
        var (x, y, z) = InstanceFieldMap.GetPosition(type, instance);
        var (xo, yo) = InstanceFieldMap.GetOrientation(type, instance);
        var position = new Vector3(x, y, z);
        var geometry = includeGeometry ? ReadGeometry(instance) : null;
        if (geometry != null)
        {
            geometry = geometry.Select(point =>
            {
                var world = point + position;
                if (tiles != null && AreaWalkmesh.GroundHeightAt(tiles, world.X, world.Y) is { } floor)
                    world = new Vector3(world.X, world.Y, floor);
                return world;
            }).ToArray();
        }

        var appearance = resolvers.ResolveInstance(type, instance);
        var visualTransform = InstanceFieldMap.GetVisualTransform(instance);
        if (appearance.ModelCorrection is { } correction)
            visualTransform = correction * visualTransform;
        return new InstanceMarker
        {
            Kind = kind,
            ListIndex = listIndex,
            TemplateResRef = InstanceFieldMap.GetTemplateResRef(type, instance),
            Tag = InstanceFieldMap.GetTag(instance),
            Position = position,
            Orientation = new Vector2(xo, yo),
            VisualTransform = visualTransform,
            Geometry = geometry,
            Model = appearance.Model,
            IsDoorTransition = appearance.IsDoorTransition,
            TintMapOverrides = appearance.TintMapOverrides,
            SoundMinDistance = kind == InstanceMarkerKind.Sound ? instance.GetSingleOrNull("MinDistance") : null,
            SoundMaxDistance = kind == InstanceMarkerKind.Sound ? instance.GetSingleOrNull("MaxDistance") : null,
            IsPositionalSound = kind == InstanceMarkerKind.Sound && (instance.GetIntOrNull("Positional") ?? 0) != 0
        };
    }

    private static IReadOnlyList<TileDoorAnchor> BuildDoorAnchors(IReadOnlyList<TilePlacement> tiles,
        TilesetDefinition? tileset)
    {
        if (tileset == null)
            return Array.Empty<TileDoorAnchor>();
        var anchors = new List<TileDoorAnchor>();
        foreach (var placement in tiles)
        {
            if (placement.TileId < 0 || placement.TileId >= tileset.Tiles.Count)
                continue;
            var doors = tileset.Tiles[placement.TileId].Doors;
            for (var index = 0; index < doors.Count; index++)
            {
                var door = doors[index];
                var local = new Vector3((float)door.X, (float)door.Y, (float)door.Z);
                var heading = (float)(door.Orientation * Math.PI / 180.0) + placement.Orientation * (float)(Math.PI / 2.0);
                anchors.Add(new TileDoorAnchor
                {
                    TileIndex = placement.TileIndex, DoorIndex = index, Type = door.Type,
                    Position = Vector3.Transform(local, placement.Transform),
                    Orientation = new Vector2(MathF.Cos(heading), MathF.Sin(heading))
                });
            }
        }
        return anchors;
    }

    private static IReadOnlyList<Vector3>? ReadGeometry(JsonGffStruct instance)
    {
        var points = instance.GetListOrEmpty("Geometry");
        if (points.Count == 0)
            return null;
        var result = new List<Vector3>(points.Count);
        foreach (var point in points)
            result.Add(new Vector3(point.GetSingleOrNull("PointX") ?? 0f,
                point.GetSingleOrNull("PointY") ?? 0f, point.GetSingleOrNull("PointZ") ?? 0f));
        return result;
    }
}
