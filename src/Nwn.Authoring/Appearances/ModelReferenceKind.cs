// SPDX-License-Identifier: MIT
namespace Nwn.Authoring.Appearances;

/// <summary>How a blueprint's preview model is assembled.</summary>
public enum ModelReferenceKind
{
    /// <summary>No model could be resolved; <see cref="ModelReference.Status"/> explains why.</summary>
    None,

    /// <summary>A single MDL resref (<see cref="ModelReference.ModelResRef"/>) is parsed and rendered directly.</summary>
    Simple,

    /// <summary>
    /// A segmented player-body model: a skeleton (<see cref="ModelReference.SkeletonResRef"/>) plus
    /// per-bone body parts (<see cref="ModelReference.Parts"/>), composed at render time.
    /// </summary>
    Segmented,

    /// <summary>
    /// A composite item's fixed-position part models (a ModelType 2 weapon's bottom/middle/top),
    /// merged with no skeleton at render time.
    /// </summary>
    ItemComposite,
}
