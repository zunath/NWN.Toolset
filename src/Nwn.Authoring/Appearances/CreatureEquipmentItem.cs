// SPDX-License-Identifier: MIT
using Nwn.Authoring.Documents.NimGff;

namespace Nwn.Authoring.Appearances;

/// <summary>An equipped item resolved from its embedded instance or referenced UTI blueprint.</summary>
public sealed record CreatureEquipmentItem(
    CreatureEquipmentSlot Slot,
    JsonGffStruct Item,
    string? BlueprintResRef,
    bool IsEmbeddedInstance);
