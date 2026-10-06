using Nwn.Authoring.Resources;

namespace Nwn.Toolset.Avalonia.Explorer.Workflow;

/// <summary>
/// What a host lists in Module Contents: which sections exist, which resources each holds, and what they
/// are called. Where the resources live and how names are resolved is the host's business.
/// </summary>
public interface IModuleExplorerContentSource
{
    /// <summary>
    /// Raised when one resource was created, saved, renamed or deleted. Move history naming a resource
    /// that no longer exists is dropped, and a content search over that type is re-run.
    /// </summary>
    event Action<ModuleResourceType, string>? ResourceChanged;

    /// <summary>Raised when the lists or names changed, so the tree is rebuilt.</summary>
    event Action? ContentChanged;

    /// <summary>The tabs, in the order the panel shows them.</summary>
    IReadOnlyList<ExplorerSection> Sections { get; }

    /// <summary>Whether a module is open, so resources can be listed, created and deleted.</summary>
    bool IsModuleOpen { get; }

    /// <summary>Every resource of a section's type. Empty when no module is open.</summary>
    IReadOnlyList<ExplorerItem> Items(ModuleResourceType type);

    /// <summary>How many resources a tab shows; called for every tab on every refresh, so keep it cheap.</summary>
    int Count(ModuleResourceType type);

    /// <summary>Whether a resource of this type still exists.</summary>
    bool Exists(ModuleResourceType type, string resRef);
}
