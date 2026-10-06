// SPDX-License-Identifier: MIT
using System.Globalization;
using System.Text;
using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Documents.NimGff;

namespace Nwn.Authoring.Appearances;

/// <summary>Reads native creature equipment and body-part precedence without selecting render policy.</summary>
public static class CreatureEquipmentResolver
{
    private const string EquipmentField = "Equip_ItemList";

    public static CreatureEquipmentProjection Resolve(
        JsonGffStruct creature,
        Func<string, JsonGffStruct?>? itemBlueprintLoader = null)
    {
        ArgumentNullException.ThrowIfNull(creature);
        var items = creature.GetListOrEmpty(EquipmentField);
        return new(
            ResolveSlot(items, CreatureEquipmentSlot.Head, itemBlueprintLoader),
            ResolveSlot(items, CreatureEquipmentSlot.Chest, itemBlueprintLoader),
            ResolveSlot(items, CreatureEquipmentSlot.RightHand, itemBlueprintLoader),
            ResolveSlot(items, CreatureEquipmentSlot.LeftHand, itemBlueprintLoader),
            ResolveSlot(items, CreatureEquipmentSlot.Cloak, itemBlueprintLoader));
    }

    public static int ResolveBodyPartNumber(int creatureNumber, int armorNumber)
    {
        if (creatureNumber == 0) return 0;
        return armorNumber > 0 ? armorNumber : creatureNumber;
    }

    public static string? GetBlueprintResRef(JsonGffStruct item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.GetStringOrNull("EquippedRes") ?? item.GetStringOrNull("TemplateResRef");
    }

    public static IReadOnlyList<string> GetVisibleBlueprintResRefs(JsonGffStruct creature)
    {
        ArgumentNullException.ThrowIfNull(creature);
        var visibleSlots = new HashSet<int>(Enum.GetValues<CreatureEquipmentSlot>().Select(slot => (int)slot));
        return Array.AsReadOnly(creature.GetListOrEmpty(EquipmentField)
            .Where(item => visibleSlots.Contains(ParseStructId(item.RawStructId)))
            .Select(GetBlueprintResRef)
            .Where(resRef => !string.IsNullOrWhiteSpace(resRef))
            .Select(resRef => resRef!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray());
    }

    private static CreatureEquipmentItem? ResolveSlot(
        IReadOnlyList<JsonGffStruct> items,
        CreatureEquipmentSlot slot,
        Func<string, JsonGffStruct?>? itemBlueprintLoader)
    {
        var equipped = items.FirstOrDefault(item => ParseStructId(item.RawStructId) == (int)slot);
        if (equipped is null) return null;

        var blueprintResRef = GetBlueprintResRef(equipped);
        if (HasEmbeddedItemData(equipped))
            return IsDegenerateEmbeddedItem(equipped) ? null : new(slot, equipped, blueprintResRef, true);

        if (string.IsNullOrWhiteSpace(blueprintResRef) || itemBlueprintLoader is null)
            return null;
        var blueprint = itemBlueprintLoader(blueprintResRef);
        return blueprint is null ? null : new(slot, blueprint, blueprintResRef, false);
    }

    private static bool HasEmbeddedItemData(JsonGffStruct item) =>
        item.GetIntOrNull("BaseItem").HasValue ||
        item.GetIntOrNull("ArmorPart_Torso").HasValue ||
        item.GetIntOrNull("ModelPart1").HasValue;

    private static bool IsDegenerateEmbeddedItem(JsonGffStruct item)
    {
        if (!string.IsNullOrWhiteSpace(item.GetStringOrNull("TemplateResRef"))) return false;
        if ((item.GetIntOrNull("BaseItem") ?? 0) != 0 || item.GetIntOrNull("ArmorPart_Torso").HasValue) return false;
        return (item.GetIntOrNull("ModelPart1") ?? 0) == 0 &&
            (item.GetIntOrNull("ModelPart2") ?? 0) == 0 &&
            (item.GetIntOrNull("ModelPart3") ?? 0) == 0;
    }

    private static int ParseStructId(byte[]? raw) => raw is not null &&
        int.TryParse(Encoding.ASCII.GetString(raw), NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
            ? id : -1;
}
