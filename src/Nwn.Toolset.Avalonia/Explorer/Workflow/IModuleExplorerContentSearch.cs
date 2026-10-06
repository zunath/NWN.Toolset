using Nwn.Authoring.Resources;

namespace Nwn.Toolset.Avalonia.Explorer.Workflow;

/// <summary>
/// An optional full-text search over resource content, such as what is said in conversations. Name and
/// resref matches appear immediately; content matches join them once a debounced background scan lands.
/// </summary>
public interface IModuleExplorerContentSearch
{
    /// <summary>Whether this type's content can be searched.</summary>
    bool Supports(ModuleResourceType type);

    /// <summary>What the panel says while a scan runs, e.g. "Searching dialogue...".</summary>
    string SearchingLabel(ModuleResourceType type);

    /// <summary>What the status line says when a scan fails.</summary>
    string FailureMessage(ModuleResourceType type, Exception exception);

    /// <summary>
    /// Captures, on the UI thread, everything the scan needs - including snapshots of unsaved editor
    /// state - so the worker never touches live UI-owned objects. Null when nothing can be searched now.
    /// </summary>
    IModuleExplorerContentSearchScan? Prepare(ModuleResourceType type, string query);
}
