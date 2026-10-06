using Nwn.Authoring.Resources;

namespace Nwn.Toolset.Avalonia.Explorer.Workflow;

/// <summary>
/// The host's logical resource deletion: which files make up a resource, the transaction that removes
/// them, and the reservation that keeps editors from opening it meanwhile.
/// </summary>
public interface IModuleExplorerDeletion
{
    /// <summary>Whether this type's resources can be deleted from the panel.</summary>
    bool CanDelete(ModuleResourceType type);

    /// <summary>
    /// Captures what the delete will remove. Throws, with the reason shown to the builder, when the
    /// resource cannot be deleted.
    /// </summary>
    IModuleExplorerPreparedDeletion Prepare(ModuleResourceType type, string resRef);

    /// <summary>
    /// Reserves the module for a delete until disposed, blocking every editor-opening route. Null when a
    /// module-wide operation holds it.
    /// </summary>
    IDisposable? TryReserve();

    /// <summary>Called once the files are gone, so the host can drop the resource from its catalog.</summary>
    void Deleted(ModuleResourceType type, string resRef);
}
