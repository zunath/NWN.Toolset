// SPDX-License-Identifier: MIT
using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Resources;

namespace Nwn.Authoring.Appearances;

/// <summary>
/// Resolves the preview model a blueprint selects from its appearance fields and the host's 2DA data,
/// headlessly. It never loads a model, texture or any other resource: the result names resrefs and, for
/// segmented creatures, the skeleton and part list, for the host's renderer to compose.
/// </summary>
/// <remarks>
/// Creatures (UTC) resolve through appearance.2da, items (UTI) through baseitems.2da, placeables (UTP)
/// through placeables.2da, waypoints (UTW) through waypoints.2da, and doors (UTD) through genericdoors.2da
/// or, when the specific Appearance field is non-zero, doortypes.2da.
/// </remarks>
public static class ModelReferenceResolver
{
    /// <summary>
    /// Returns a <see cref="ModelReferenceKind.None"/> reference (never throws, never null) when the type is
    /// not previewable, a needed table is absent, or the appearance cannot be resolved.
    /// </summary>
    /// <param name="type">The blueprint type.</param>
    /// <param name="root">The blueprint's root struct.</param>
    /// <param name="host">The host supplying 2DA data and policy.</param>
    /// <param name="armorPreviewFemale">An armor or cloak item is dressed on the female mannequin rather than the male one.</param>
    public static ModelReference Resolve(
        ModuleResourceType type,
        JsonGffStruct root,
        IModelResolutionHost host,
        bool armorPreviewFemale = false)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(host);

        return type switch
        {
            ModuleResourceType.Utc => CreatureModelResolver.Resolve(root, host),
            ModuleResourceType.Utp => ObjectModelResolver.ResolvePlaceable(root, host),
            ModuleResourceType.Utd => ObjectModelResolver.ResolveDoor(root, host),
            ModuleResourceType.Utw => ObjectModelResolver.ResolveWaypoint(root, host),
            ModuleResourceType.Uti => ItemModelResolver.Resolve(root, host, armorPreviewFemale),
            _ => ModelReference.NoneWith("No model preview for this blueprint type."),
        };
    }
}
