// SPDX-License-Identifier: MIT
using Nwn.Authoring.Documents.NimGff;

namespace Nwn.Authoring.Appearances;

/// <summary>
/// One resolved body or equipment part: its attachment type, MDL resref, any item-specific PLT palette
/// choices, and an optional texture selected independently of geometry. Equipment palettes live here
/// because a cloak and the chest armor beneath it can intentionally use different dye rows, and
/// cloakmodel.2da lets several appearances share geometry while selecting different surfaces.
/// </summary>
/// <param name="PartType">The part attachment type, such as <c>chest</c>, <c>head</c>, <c>helmet</c> or <c>weaponr</c>.</param>
/// <param name="ModelResRef">The part's MDL resref.</param>
/// <param name="LayerColorIndices">PLT layer id to palette-row index for this part alone; null when the part uses the model-wide palette.</param>
/// <param name="TextureResRef">A texture that replaces the model's authored bitmap, or null.</param>
/// <param name="UsesItemTintOverrides">The part's meshes belong to an item whose per-item tint overrides apply.</param>
/// <param name="TintSourceItem">The item blueprint whose per-item tint data a host applies to this part, or null.</param>
/// <param name="IsItemSupplied">The part's model number came from an item (armor part, robe or equipment) rather than the creature itself.</param>
public readonly record struct ModelPartReference(
    string PartType,
    string ModelResRef,
    IReadOnlyDictionary<int, int>? LayerColorIndices = null,
    string? TextureResRef = null,
    bool UsesItemTintOverrides = false,
    JsonGffStruct? TintSourceItem = null,
    bool IsItemSupplied = false);
