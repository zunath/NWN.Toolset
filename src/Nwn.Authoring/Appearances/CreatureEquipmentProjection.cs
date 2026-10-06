// SPDX-License-Identifier: MIT
namespace Nwn.Authoring.Appearances;

/// <summary>Resolved equipment that can alter the visible creature model.</summary>
public sealed record CreatureEquipmentProjection(
    CreatureEquipmentItem? Helmet,
    CreatureEquipmentItem? Armor,
    CreatureEquipmentItem? RightHand,
    CreatureEquipmentItem? LeftHand,
    CreatureEquipmentItem? Cloak)
{
    public IReadOnlyList<CreatureEquipmentItem> VisibleItems { get; } = Array.AsReadOnly(
        new[] { Helmet, Armor, RightHand, LeftHand, Cloak }.OfType<CreatureEquipmentItem>().ToArray());
}
