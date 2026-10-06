namespace Nwn.Toolset.Avalonia.Explorer.Workflow;

/// <summary>
/// One delete captured before the builder confirms it, so the commit can refuse files that changed while
/// the confirmation was on screen.
/// </summary>
public interface IModuleExplorerPreparedDeletion
{
    /// <summary>
    /// Deletes exactly the prepared generation. Runs on a worker thread; throws, without deleting, when
    /// anything changed.
    /// </summary>
    ModuleExplorerDeletionResult Commit();
}
