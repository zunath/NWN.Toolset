// SPDX-License-Identifier: MIT
using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Documents.NimGff;

namespace Nwn.Authoring.Appearances;

/// <summary>
/// Resolves a creature blueprint's model. A simple appearance (appearance.2da MODELTYPE other than
/// <c>P</c>) holds the model resref in its RACE column. A segmented player-body appearance resolves to a
/// skeleton plus body parts named <c>p{gender}{race}{phenotype}_{part}{number}</c>, with the equipped chest
/// armor's ArmorPart_* overrides, a robe, visible equipment and any host attachments.
/// </summary>
internal static class CreatureModelResolver
{
    internal static ModelReference Resolve(JsonGffStruct root, IModelResolutionHost host)
    {
        if (!host.IsTableAvailable(ModelResolutionTable.CreatureAppearances))
            return ModelReference.NoneWith("Creature preview unavailable (appearance data not loaded).");

        var appearanceId = root.GetIntOrNull("Appearance_Type") ?? -1;
        var row = host.GetCreatureAppearance(appearanceId);
        if (row == null)
            return ModelReference.NoneWith($"Unknown appearance id {appearanceId}.");

        var equipment = CreatureEquipmentResolver.Resolve(root, host.LoadItemBlueprint);
        var armor = equipment.Armor?.Item;

        if (string.Equals(row.ModelType, "P", StringComparison.OrdinalIgnoreCase))
        {
            var prefix = SegmentedPrefix(root, row);
            if (prefix == null)
                return ModelReference.NoneWith($"{row.DisplayName}: segmented appearance has no race letter.");

            var visibleEquipment = AddAttachments(
                root, VisibleEquipmentModelResolver.Resolve(equipment, host, prefix), armor, host);
            return ResolveSegmented(root, row, prefix, host, visibleEquipment, armor);
        }

        var modelResRef = row.Race;
        if (string.IsNullOrWhiteSpace(modelResRef))
            return ModelReference.NoneWith($"{row.DisplayName}: no model ResRef in appearance.2da.");

        var simpleParts = AddAttachments(
            root, VisibleEquipmentModelResolver.Resolve(equipment, host, wearerPrefix: null), armor, host);
        return new ModelReference
        {
            Kind = ModelReferenceKind.Simple,
            Status = $"{row.DisplayName} ({modelResRef}.mdl)",
            ModelResRef = modelResRef,
            Parts = simpleParts.Parts,
            LayerColorIndices = ModelPaletteResolver.Resolve(root, null, host.FillMissingPaletteLayers),
        };
    }

    private static string? SegmentedPrefix(JsonGffStruct root, CreatureAppearanceModelRow row)
    {
        if (string.IsNullOrWhiteSpace(row.Race))
            return null;

        return ModelPartNames.Creature(
            (root.GetIntOrNull("Gender") ?? 0) == 1,
            row.Race[0],
            root.GetIntOrNull("Phenotype") ?? 0);
    }

    private static ModelReference ResolveSegmented(
        JsonGffStruct root,
        CreatureAppearanceModelRow row,
        string prefix,
        IModelResolutionHost host,
        VisibleEquipmentModels visibleEquipment,
        JsonGffStruct? armor)
    {
        var parts = new List<ModelPartReference>();

        // Robe first (armor-only; creatures have no robe body part), when its model resolves. ALL body
        // parts are still emitted alongside it: whether the robe replaces the parts it covers depends
        // on its geometry, which the renderer decides after loading the model.
        var robeNumber = armor == null ? 0 : host.ReadItemAppearanceValue(armor, "ArmorPart_Robe") ?? 0;
        if (robeNumber > 0)
        {
            var robeResRef = ModelPartNames.Body(prefix, "robe", robeNumber);
            if (host.PartModelExists(robeResRef))
            {
                parts.Add(new("robe", robeResRef,
                    UsesItemTintOverrides: true, TintSourceItem: armor, IsItemSupplied: true));
            }
        }

        var head = root.GetIntOrNull("Appearance_Head");
        if (head is > 0)
            parts.Add(new("head", ModelPartNames.Body(prefix, "head", head.Value)));

        foreach (var field in CreatureBodyPartFields.All)
        {
            if (visibleEquipment.HiddenBodyParts.Contains(field.PartType))
                continue;

            var armorNumber = armor == null
                ? 0
                : host.ReadItemAppearanceValue(armor, "ArmorPart_" + field.ArmorKey) ?? 0;
            var number = CreatureEquipmentResolver.ResolveBodyPartNumber(
                root.GetIntOrNull(field.CreatureField) ?? 0, armorNumber);
            if (number > 0)
            {
                parts.Add(new(
                    field.PartType,
                    ModelPartNames.Body(prefix, field.PartType, number),
                    UsesItemTintOverrides: armor != null,
                    TintSourceItem: armor,
                    IsItemSupplied: armorNumber > 0));
            }
        }

        parts.AddRange(visibleEquipment.Parts);

        if (parts.Count == 0)
            return ModelReference.NoneWith($"{row.DisplayName}: segmented creature has no body parts.");

        return new ModelReference
        {
            Kind = ModelReferenceKind.Segmented,
            Status = $"{row.DisplayName} (segmented {prefix}, {parts.Count} parts)",
            SkeletonResRef = prefix,
            Parts = parts,
            LayerColorIndices = ModelPaletteResolver.Resolve(root, armor, host.FillMissingPaletteLayers),
        };
    }

    private static VisibleEquipmentModels AddAttachments(
        JsonGffStruct creature,
        VisibleEquipmentModels visibleEquipment,
        JsonGffStruct? armor,
        IModelResolutionHost host)
    {
        var palette = ModelPaletteResolver.Resolve(creature, armor, host.FillMissingPaletteLayers);
        var attachments = host.GetCreatureAttachments(creature, armor, palette);
        if (attachments.Count == 0)
            return visibleEquipment;

        return new VisibleEquipmentModels(
            visibleEquipment.Parts.Concat(attachments).ToArray(),
            visibleEquipment.HiddenBodyParts);
    }
}
