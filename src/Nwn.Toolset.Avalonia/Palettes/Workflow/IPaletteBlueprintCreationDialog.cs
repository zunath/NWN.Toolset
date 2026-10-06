using Nwn.Authoring.Resources;

namespace Nwn.Toolset.Avalonia.Palettes.Workflow;

/// <summary>
/// A host's own blueprint creation UI, run in place of the palette's name prompt. Optional: without it, or
/// for a type it does not handle, the palette asks for a name, derives the resref and calls
/// <see cref="IPaletteBlueprintOperations.CreateAsync"/>.
/// </summary>
/// <remarks>
/// The palette still decides whether "New" is offered at all (<see cref="IPaletteBlueprintOperations.CanCreate"/>),
/// and still files the result into the selected category, reports it and, unless the dialog already did,
/// opens its editor.
/// </remarks>
public interface IPaletteBlueprintCreationDialog
{
    /// <summary>Whether this UI creates blueprints of a type; false falls back to the name prompt.</summary>
    bool Handles(ModuleResourceType type);

    /// <summary>
    /// Runs the host's creation UI and writes the blueprint. Returns
    /// <see cref="PaletteBlueprintCreation.CreatedByHost"/> naming the new resref,
    /// <see cref="PaletteBlueprintCreation.Cancelled"/> when the builder backs out, or a failure.
    /// </summary>
    Task<PaletteBlueprintCreation> CreateAsync(ModuleResourceType type, CancellationToken cancellationToken);
}
