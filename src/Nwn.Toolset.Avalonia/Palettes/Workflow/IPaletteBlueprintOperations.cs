using Nwn.Authoring.Resources;

namespace Nwn.Toolset.Avalonia.Palettes.Workflow;

/// <summary>
/// The host's blueprint storage: creating, copying and deleting blueprints, and opening their editors.
/// Every write path, lock and fingerprint is the host's.
/// </summary>
public interface IPaletteBlueprintOperations
{
    /// <summary>
    /// Whether the host can create a complete, usable blueprint of a type. Types whose editor cannot
    /// finish one stay browsable and editable but offer no "New" action.
    /// </summary>
    bool CanCreate(ModuleResourceType type);

    /// <summary>Whether the host can delete blueprints of a type; when it cannot, no delete action is offered.</summary>
    bool CanDelete(ModuleResourceType type);

    /// <summary>
    /// Writes a new blueprint named <paramref name="name"/> with resref <paramref name="resRef"/>. A host
    /// whose write is synchronous may return a completed task.
    /// </summary>
    Task<PaletteBlueprintCreation> CreateAsync(
        ModuleResourceType type,
        string resRef,
        string name,
        CancellationToken cancellationToken);

    /// <summary>
    /// Writes an independent module copy of a blueprint from either side of the palette. The source and
    /// every instance placed from it stay untouched. A host that makes the copy in an editor instead,
    /// choosing the new identity there, returns <see cref="PaletteBlueprintCopy.OpenedInEditor"/>.
    /// </summary>
    Task<PaletteBlueprintCopy> CopyAsync(
        ModuleResourceType type,
        PaletteSource source,
        string resRef,
        CancellationToken cancellationToken);

    /// <summary>
    /// Captures what is about to be deleted - before the builder confirms - so the delete can refuse a
    /// blueprint that changed while the confirmation was on screen.
    /// </summary>
    PaletteBlueprintDeletePreparation PrepareDelete(ModuleResourceType type, string resRef);

    /// <summary>Whether an editor session holds this blueprint.</summary>
    bool IsOpenInEditor(ModuleResourceType type, string resRef);

    /// <summary>Opens the blueprint's editor.</summary>
    void OpenEditor(ModuleResourceType type, string resRef);
}
