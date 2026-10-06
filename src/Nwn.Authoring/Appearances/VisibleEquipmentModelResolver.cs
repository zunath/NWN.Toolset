// SPDX-License-Identifier: MIT
using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Documents.NimGff;

namespace Nwn.Authoring.Appearances;

/// <summary>
/// Resolves the models for equipment the game draws on a creature. Ordinary right- and left-hand items
/// are held props; creature natural-weapon and stat slots are deliberately not drawn.
/// </summary>
internal static class VisibleEquipmentModelResolver
{
    /// <param name="equipment">The creature's resolved equipment.</param>
    /// <param name="host">The host supplying 2DA data and policy.</param>
    /// <param name="wearerPrefix">The wearer's segmented body prefix, or null for a creature with a single model.</param>
    internal static VisibleEquipmentModels Resolve(
        CreatureEquipmentProjection equipment,
        IModelResolutionHost host,
        string? wearerPrefix)
    {
        if (!host.IsTableAvailable(ModelResolutionTable.BaseItems))
        {
            return new VisibleEquipmentModels(
                Array.Empty<ModelPartReference>(),
                new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        }

        var parts = new List<ModelPartReference>();
        Add(parts, equipment.Helmet, "helmet", host, wearerPrefix);
        Add(parts, equipment.Cloak, "cloak", host, wearerPrefix);
        Add(parts, equipment.RightHand, "weaponr", host, wearerPrefix);
        Add(parts, equipment.LeftHand, "weaponl", host, wearerPrefix);
        return new VisibleEquipmentModels(parts, ResolveCloakHiddenBodyParts(equipment.Cloak?.Item, host));
    }

    private static IReadOnlySet<string> ResolveCloakHiddenBodyParts(JsonGffStruct? cloak, IModelResolutionHost host)
    {
        var hidden = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var appearance = cloak == null ? null : host.ReadItemAppearanceValue(cloak, "ModelPart1");
        var mapping = appearance is { } value ? host.GetCloak(value) : null;
        if (mapping?.HideLeftShoulder == true) hidden.Add("shol");
        if (mapping?.HideRightShoulder == true) hidden.Add("shor");
        return hidden;
    }

    private static void Add(
        ICollection<ModelPartReference> destination,
        CreatureEquipmentItem? equipped,
        string attachmentType,
        IModelResolutionHost host,
        string? wearerPrefix)
    {
        var item = equipped?.Item;
        if (item == null)
            return;

        if (attachmentType == "weaponl" && item.GetIntOrNull("BaseItem") is { } baseItem &&
            host.IsShield(baseItem, host.GetBaseItem(baseItem)?.ItemClass ?? string.Empty))
        {
            attachmentType = "shield";
        }

        var reference = ItemModelResolver.Resolve(item, host, female: false);
        if (attachmentType == "cloak")
        {
            AddCloak(destination, item, reference, host, wearerPrefix);
            return;
        }

        if (reference.Kind == ModelReferenceKind.ItemComposite)
        {
            foreach (var part in reference.Parts)
                destination.Add(Attachment(attachmentType, part.ModelResRef, reference, item));
            return;
        }

        if (reference is { Kind: ModelReferenceKind.Simple, IsFallbackModel: false } &&
            !string.IsNullOrWhiteSpace(reference.ModelResRef))
        {
            destination.Add(Attachment(attachmentType, reference.ModelResRef, reference, item));
        }
    }

    /// <summary>
    /// A cloak resolves on a default mannequin; its cloak part is retargeted to the wearer's own body
    /// prefix, since cloak geometry is skinned to the skeleton it hangs from.
    /// </summary>
    private static void AddCloak(
        ICollection<ModelPartReference> destination,
        JsonGffStruct item,
        ModelReference reference,
        IModelResolutionHost host,
        string? wearerPrefix)
    {
        foreach (var part in reference.Parts.Where(
                     part => part.PartType.Equals("cloak", StringComparison.OrdinalIgnoreCase)))
        {
            var modelResRef = part.ModelResRef;
            var textureResRef = part.TextureResRef;
            if (!string.IsNullOrWhiteSpace(wearerPrefix))
            {
                modelResRef = ModelPartNames.RewriteCloakPrefix(modelResRef, wearerPrefix)!;
                textureResRef = ModelPartNames.RewriteCloakPrefix(textureResRef, wearerPrefix);
            }

            if (host.PartModelExists(modelResRef))
            {
                destination.Add(new ModelPartReference(
                    "cloak", modelResRef, reference.LayerColorIndices, textureResRef,
                    UsesItemTintOverrides: true,
                    TintSourceItem: item,
                    IsItemSupplied: true));
            }
        }
    }

    private static ModelPartReference Attachment(
        string attachmentType, string modelResRef, ModelReference reference, JsonGffStruct item) =>
        new(attachmentType, modelResRef, reference.LayerColorIndices,
            UsesItemTintOverrides: true,
            TintSourceItem: item,
            IsItemSupplied: true);
}
