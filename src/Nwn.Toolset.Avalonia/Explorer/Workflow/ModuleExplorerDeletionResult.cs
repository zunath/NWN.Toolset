namespace Nwn.Toolset.Avalonia.Explorer.Workflow;

/// <summary>What a committed delete removed, and anything its cleanup could not finish.</summary>
/// <param name="DeletedPaths">Every file the delete removed, for the log.</param>
/// <param name="CleanupWarnings">Problems removing temporary backups; the delete itself still committed.</param>
public sealed record ModuleExplorerDeletionResult(
    IReadOnlyList<string> DeletedPaths,
    IReadOnlyList<string> CleanupWarnings);
