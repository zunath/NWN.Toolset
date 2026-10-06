// SPDX-License-Identifier: MIT
using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Documents.NimGff;

namespace Nwn.Authoring.Appearances;

/// <summary>Resolves the models of placeables, waypoints and doors from their appearance rows.</summary>
internal static class ObjectModelResolver
{
    internal static ModelReference ResolvePlaceable(JsonGffStruct root, IModelResolutionHost host)
    {
        if (!host.IsTableAvailable(ModelResolutionTable.Placeables))
            return ModelReference.NoneWith("Placeable preview unavailable (placeable data not loaded).");

        var appearanceId = root.GetIntOrNull("Appearance") ?? -1;
        var row = host.GetPlaceable(appearanceId);
        if (row == null)
            return ModelReference.NoneWith($"Unknown placeable appearance id {appearanceId}.");
        if (string.IsNullOrWhiteSpace(row.ModelName))
            return ModelReference.NoneWith($"{row.DisplayName}: no model in placeables.2da.");

        return new ModelReference
        {
            Kind = ModelReferenceKind.Simple,
            Status = $"{row.DisplayName} ({row.ModelName}.mdl)",
            ModelResRef = row.ModelName,
        };
    }

    /// <summary>A waypoint's marker model. Unlike placeables.2da the row's RESREF is the model.</summary>
    internal static ModelReference ResolveWaypoint(JsonGffStruct root, IModelResolutionHost host)
    {
        if (!host.IsTableAvailable(ModelResolutionTable.Waypoints))
            return ModelReference.NoneWith("Waypoint preview unavailable (waypoint data not loaded).");

        var appearanceId = root.GetIntOrNull("Appearance") ?? -1;
        var row = host.GetWaypoint(appearanceId);
        if (row == null)
            return ModelReference.NoneWith($"Unknown waypoint appearance {appearanceId}.");
        if (string.IsNullOrWhiteSpace(row.ModelName))
            return ModelReference.NoneWith($"{row.DisplayName}: no model in waypoint.2da.");

        return new ModelReference
        {
            Kind = ModelReferenceKind.Simple,
            Status = $"{row.DisplayName} ({row.ModelName}.mdl)",
            ModelResRef = row.ModelName,
        };
    }

    /// <summary>
    /// Appearance names a specific doortypes.2da model when non-zero. Otherwise GenericType_New (or legacy
    /// GenericType) indexes genericdoors.2da. A row declaring <c>VisibleModel=0</c> is a transition plane.
    /// </summary>
    internal static ModelReference ResolveDoor(JsonGffStruct root, IModelResolutionHost host)
    {
        if (!host.IsTableAvailable(ModelResolutionTable.Doors))
            return ModelReference.NoneWith("Door preview unavailable (door-type data not loaded).");

        // Door ids are read as full 32-bit values: a Dword sentinel such as 0xFFFFFFFF is an id no
        // 2DA row can have, and must resolve to "unknown" rather than fail.
        var specificId = ReadId(root, "Appearance") ?? 0;
        var specific = specificId > 0 ? Lookup(specificId, host.GetSpecificDoor) : null;
        var genericId = ReadId(root, "GenericType_New")
                        ?? ReadId(root, "GenericType")
                        ?? 0;
        var generic = specificId == 0 ? Lookup(genericId, host.GetGenericDoor) : null;
        var displayName = specific?.DisplayName ?? generic?.DisplayName;
        var model = specific?.Model ?? generic?.Model;
        var visibleModel = specific?.VisibleModel ?? generic?.VisibleModel ?? true;
        var table = specific != null ? "doortypes.2da" : "genericdoors.2da";

        if (displayName == null)
        {
            return ModelReference.NoneWith(
                $"Unknown {(specificId > 0 ? "specific" : "generic")} door type " +
                $"{(specificId > 0 ? specificId : genericId)}.");
        }

        if (string.IsNullOrWhiteSpace(model))
            return ModelReference.NoneWith($"{displayName}: no model in {table}.");

        return new ModelReference
        {
            Kind = ModelReferenceKind.Simple,
            Status = $"{displayName} ({model}.mdl)",
            ModelResRef = model,
            IsDoorTransition = !visibleModel,
        };
    }

    private static long? ReadId(JsonGffStruct root, string field) =>
        root.TryGet(field, out var value) ? value.GetInteger() : null;

    private static DoorModelRow? Lookup(long id, Func<int, DoorModelRow?> lookup) =>
        id is >= 0 and <= int.MaxValue ? lookup((int)id) : null;
}
