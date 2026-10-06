// SPDX-License-Identifier: MIT
namespace Nwn.Authoring.Appearances;

/// <summary>Native creature field, equipped-armor key and skeleton attachment category.</summary>
public readonly record struct CreatureBodyPartField(string CreatureField, string ArmorKey, string PartType);
