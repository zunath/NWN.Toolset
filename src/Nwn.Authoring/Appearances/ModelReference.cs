// SPDX-License-Identifier: MIT
namespace Nwn.Authoring.Appearances;

/// <summary>
/// The resolved preview-model description for a blueprint. Pure data: it names resrefs and (for segmented
/// creatures) the skeleton and part list, but never touches the resource index, MDL parser or GL.
/// </summary>
public sealed class ModelReference
{
    public required ModelReferenceKind Kind { get; init; }

    /// <summary>A human-readable note for status display (the appearance label, or why nothing resolved).</summary>
    public required string Status { get; init; }

    /// <summary>The single model resref for <see cref="ModelReferenceKind.Simple"/>; null otherwise.</summary>
    public string? ModelResRef { get; init; }

    /// <summary>
    /// The meshes in <see cref="ModelResRef"/> belong to the item being previewed. This is separate from
    /// <see cref="ModelPartReference.UsesItemTintOverrides"/> because a simple ModelType 0/1 item has no
    /// composed part record.
    /// </summary>
    public bool RootUsesItemTintOverrides { get; init; }

    /// <summary>
    /// The resolved door row declares <c>VisibleModel=0</c>. These are area-transition planes: invisible
    /// at runtime, but drawn translucently by toolsets from the model's hidden selection geometry.
    /// </summary>
    public bool IsDoorTransition { get; init; }

    /// <summary>The model is the host's stand-in for an item with no model of its own (see <see cref="IModelResolutionHost.FallbackItemModelResRef"/>).</summary>
    public bool IsFallbackModel { get; init; }

    /// <summary>The skeleton/supermodel resref for <see cref="ModelReferenceKind.Segmented"/>; null otherwise.</summary>
    public string? SkeletonResRef { get; init; }

    /// <summary>
    /// Body parts for <see cref="ModelReferenceKind.Segmented"/>, or visible equipment attached to a
    /// <see cref="ModelReferenceKind.Simple"/> creature.
    /// </summary>
    public IReadOnlyList<ModelPartReference> Parts { get; init; } = Array.Empty<ModelPartReference>();

    /// <summary>
    /// PLT layer id to palette-row index for segmented creature textures. Empty for models that do not
    /// carry creature or armor palette choices.
    /// </summary>
    public IReadOnlyDictionary<int, int> LayerColorIndices { get; init; } = new Dictionary<int, int>();

    public static ModelReference NoneWith(string status) =>
        new() { Kind = ModelReferenceKind.None, Status = status };
}
