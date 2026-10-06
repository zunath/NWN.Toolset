// SPDX-License-Identifier: MIT
using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Documents.NimGff;

namespace Nwn.Authoring.Appearances;

/// <summary>Resolves an item blueprint's preview model by its base item's ModelType.</summary>
/// <remarks>
/// A ModelType 2 (composite) weapon resolves to its three fixed-position bottom/middle/top parts, named
/// <c>{ItemClass}_b_{ModelPart1:D3}</c>, <c>_m_{ModelPart2:D3}</c> and <c>_t_{ModelPart3:D3}</c>. A ModelType
/// 0/1 item resolves to a single ground model <c>{ItemClass}_{ModelPart1:D3}</c> when it exists. ModelType 3
/// (armor) is assembled on a male or female mannequin from its body-part fields. An item with no model of
/// its own degrades to the host's fallback model, when it names one.
/// </remarks>
internal static class ItemModelResolver
{
    internal static ModelReference Resolve(JsonGffStruct root, IModelResolutionHost host, bool female)
    {
        if (!host.IsTableAvailable(ModelResolutionTable.BaseItems))
            return ModelReference.NoneWith("Item preview unavailable (base item data not loaded).");

        var baseItem = root.GetIntOrNull("BaseItem") ?? -1;
        var row = baseItem < 0 ? null : host.GetBaseItem(baseItem);
        if (row == null)
            return ModelReference.NoneWith($"Unknown base item {baseItem}.");

        var itemClass = row.ItemClass;
        if (string.IsNullOrWhiteSpace(itemClass))
            return ModelReference.NoneWith($"Base item {baseItem}: no item class in baseitems.2da.");

        switch (row.ModelType)
        {
            case 2:
            {
                var part1 = host.ReadItemAppearanceValue(root, "ModelPart1") ?? 0;
                var part2 = host.ReadItemAppearanceValue(root, "ModelPart2") ?? 0;
                var part3 = host.ReadItemAppearanceValue(root, "ModelPart3") ?? 0;
                var parts = new[]
                {
                    new ModelPartReference("bottom", ModelPartNames.ItemSegment(itemClass, 'b', part1)),
                    new ModelPartReference("middle", ModelPartNames.ItemSegment(itemClass, 'm', part2)),
                    new ModelPartReference("top", ModelPartNames.ItemSegment(itemClass, 't', part3)),
                };

                if (!parts.Any(part => host.PartModelExists(part.ModelResRef)))
                    return Fallback(itemClass, host, "no composite part model resolves");

                return new ModelReference
                {
                    Kind = ModelReferenceKind.ItemComposite,
                    Status = $"{itemClass} (composite {part1}-{part2}-{part3})",
                    Parts = parts,
                    LayerColorIndices = host.GetCompositeItemPalette(root),
                };
            }

            case 0:
            case 1:
            {
                var part1 = host.ReadItemAppearanceValue(root, "ModelPart1") ?? 0;

                // A cloak's own model is a skinmesh weighted to the skeleton's cloak chain: drawn by
                // itself it is a flat sheet in mid-air. Worn on the mannequin it hangs where it is
                // meant to, which is the only way to judge one.
                if (string.Equals(itemClass, ModelPartNames.CloakItemClass, StringComparison.OrdinalIgnoreCase))
                {
                    var cloakMapping = host.GetCloak(part1);
                    return ResolveCapeMannequin(
                        root, itemClass, female, host,
                        cloakMapping?.Model ?? part1,
                        cloakMapping?.Texture ?? part1);
                }

                var modelResRef = ModelPartNames.Item(itemClass, part1);
                if (!host.PartModelExists(modelResRef))
                    return Fallback(itemClass, host, $"no ground model '{modelResRef}'");

                return new ModelReference
                {
                    Kind = ModelReferenceKind.Simple,
                    Status = $"{modelResRef}.mdl",
                    ModelResRef = modelResRef,
                    RootUsesItemTintOverrides = true,
                    LayerColorIndices = ModelPaletteResolver.ResolveItem(root, host.FillMissingPaletteLayers),
                };
            }

            case 3:
                return ResolveArmorMannequin(root, itemClass, female, host);

            default:
                return Fallback(itemClass, host, $"unsupported model type {row.ModelType}");
        }
    }

    /// <summary>
    /// The model NWN itself drops on the ground for an item with no ground model of its own: the loot bag
    /// (baseitems.2da's near-universal DefaultModel), when the host names one.
    /// </summary>
    private static ModelReference Fallback(string itemClass, IModelResolutionHost host, string why)
    {
        var fallback = host.FallbackItemModelResRef;
        if (string.IsNullOrWhiteSpace(fallback) || !host.PartModelExists(fallback))
            return ModelReference.NoneWith($"{itemClass}: {why}, and no loot bag model.");

        return new ModelReference
        {
            Kind = ModelReferenceKind.Simple,
            Status = $"{itemClass}: {why} - showing the loot bag.",
            ModelResRef = fallback,
            IsFallbackModel = true,
        };
    }

    /// <summary>
    /// Dresses a default human mannequin with the armor blueprint's own parts. The mannequin's naked
    /// baseline is part 1 for every body piece (head included) and none for the shoulders; each
    /// ArmorPart_* the blueprint carries overrides its slot, the robe is added when one is set, and the
    /// dye channels come from the blueprint's color fields.
    /// </summary>
    private static ModelReference ResolveArmorMannequin(
        JsonGffStruct root, string itemClass, bool female, IModelResolutionHost host)
    {
        var prefix = MannequinPrefix(female);
        var parts = new List<ModelPartReference>();

        var robeNumber = host.ReadItemAppearanceValue(root, "ArmorPart_Robe") ?? 0;
        if (robeNumber > 0)
        {
            var robeResRef = ModelPartNames.Body(prefix, "robe", robeNumber);
            if (host.PartModelExists(robeResRef))
                parts.Add(new("robe", robeResRef, UsesItemTintOverrides: true, IsItemSupplied: true));
        }

        parts.Add(new("head", ModelPartNames.Body(prefix, "head", 1)));

        foreach (var field in CreatureBodyPartFields.All)
        {
            var armorValue = host.ReadItemAppearanceValue(root, "ArmorPart_" + field.ArmorKey) ?? 0;

            // Unlike a dressed creature (where a creature part of 0 means "this body has no such
            // part"), the mannequin exists to SHOW the armor: an armor part always wins, and only the
            // armor-less slots fall back to the bare body (shoulders have no bare-body piece at all).
            var number = armorValue > 0
                ? armorValue
                : field.PartType is "shol" or "shor" ? 0 : 1;
            if (number > 0)
            {
                parts.Add(new(
                    field.PartType,
                    ModelPartNames.Body(prefix, field.PartType, number),
                    UsesItemTintOverrides: true,
                    IsItemSupplied: armorValue > 0));
            }
        }

        return new ModelReference
        {
            Kind = ModelReferenceKind.Segmented,
            Status = MannequinStatus(itemClass, female, prefix),
            SkeletonResRef = prefix,
            Parts = parts,

            // The item struct carries no Color_* creature fields, so skin/hair fall to palette row 0;
            // the armor dye channels come from the blueprint itself.
            LayerColorIndices = ModelPaletteResolver.ResolveItem(root, host.FillMissingPaletteLayers),
        };
    }

    /// <summary>A cape dressed on a plain mannequin: the bare body, plus the cloak at the number the blueprint names.</summary>
    private static ModelReference ResolveCapeMannequin(
        JsonGffStruct root,
        string itemClass,
        bool female,
        IModelResolutionHost host,
        int cloakNumber,
        int cloakTextureNumber)
    {
        var prefix = MannequinPrefix(female);
        var cloakResRef = ModelPartNames.Cloak(prefix, cloakNumber);
        if (!host.PartModelExists(cloakResRef))
            return Fallback(itemClass, host, $"no cloak model '{cloakResRef}'");

        var parts = new List<ModelPartReference>
        {
            new(
                "cloak",
                cloakResRef,
                TextureResRef: ModelPartNames.Cloak(prefix, cloakTextureNumber),
                UsesItemTintOverrides: true),
            new("head", ModelPartNames.Body(prefix, "head", 1)),
        };
        foreach (var field in CreatureBodyPartFields.All)
        {
            // The body is here to hang the cape on, so it stays plain: part 1 everywhere it exists,
            // and shoulders (which have no bare-body piece) left off.
            if (field.PartType is "shol" or "shor")
                continue;

            parts.Add(new(field.PartType, ModelPartNames.Body(prefix, field.PartType, 1)));
        }

        return new ModelReference
        {
            Kind = ModelReferenceKind.Segmented,
            Status = MannequinStatus(itemClass, female, prefix),
            SkeletonResRef = prefix,
            Parts = parts,
            LayerColorIndices = ModelPaletteResolver.ResolveItem(root, host.FillMissingPaletteLayers),
        };
    }

    private static string MannequinPrefix(bool female) =>
        female ? ModelPartNames.FemaleMannequinPrefix : ModelPartNames.MaleMannequinPrefix;

    private static string MannequinStatus(string itemClass, bool female, string prefix) =>
        $"{itemClass} on a {(female ? "female" : "male")} mannequin ({prefix})";
}
