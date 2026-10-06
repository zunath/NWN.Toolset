using Nwn.Authoring.Resources;

namespace Nwn.Toolset.Avalonia.Explorer.Workflow;

/// <summary>The host's editors: opening, compiling, and what a delete has to know about open sessions.</summary>
public interface IModuleExplorerEditors
{
    /// <summary>Whether this type's resources can be opened at all.</summary>
    bool CanOpen(ModuleResourceType type);

    void Open(ModuleResourceType type, string resRef);

    /// <summary>Whether an editor session holds this resource.</summary>
    bool IsOpen(ModuleResourceType type, string resRef);

    /// <summary>
    /// Whether the module properties editor is open. Deleting an area edits the module's area list, which
    /// that editor would save back.
    /// </summary>
    bool IsModulePropertiesOpen { get; }

    /// <summary>Discards an editor whose resource was just deleted. False when it could not be closed.</summary>
    bool TryCloseForDeletion(ModuleResourceType type, string resRef);

    /// <summary>Compiles a resource of a compilable section without opening it.</summary>
    Task CompileAsync(ModuleResourceType type, string resRef);
}
