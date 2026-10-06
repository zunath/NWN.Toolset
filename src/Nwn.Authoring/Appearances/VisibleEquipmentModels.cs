// SPDX-License-Identifier: MIT
namespace Nwn.Authoring.Appearances;

/// <summary>Equipment attachments a creature draws, and the body parts its cloak hides.</summary>
internal readonly record struct VisibleEquipmentModels(
    IReadOnlyList<ModelPartReference> Parts,
    IReadOnlySet<string> HiddenBodyParts);
