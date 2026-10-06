namespace Nwn.Toolset.Avalonia.Palettes.Workflow;

/// <summary>
/// One prepared blueprint deletion: the generation the builder is about to confirm, and the host's
/// guarded delete of exactly that generation.
/// </summary>
/// <remarks>
/// The palette disposes the deletion once the whole command has finished, including removing the
/// blueprint from its categories. A host may hold the write lease <see cref="Commit"/> takes until then,
/// so that sidecar update happens under the same lease as the delete.
/// </remarks>
public interface IPaletteBlueprintDeletion : IDisposable
{
    /// <summary>What the confirmation names as the thing being deleted, such as a file name.</summary>
    string DisplayName { get; }

    /// <summary>Where the blueprint lived, for the log.</summary>
    string Location { get; }

    /// <summary>
    /// False when the module this deletion was prepared against is no longer the open one; the palette
    /// then drops the request silently.
    /// </summary>
    bool IsCurrent { get; }

    /// <summary>
    /// Deletes the blueprint if it is still the generation that was prepared, and removes it from the
    /// host's catalog. Refuses, without deleting, when it changed or a module-wide operation started.
    /// </summary>
    PaletteOperationResult Commit();
}
