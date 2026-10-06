namespace Nwn.Toolset.Avalonia.Explorer.Workflow;

/// <summary>One prepared content scan. Runs on a worker thread and must honour cancellation.</summary>
public interface IModuleExplorerContentSearchScan
{
    /// <summary>The resrefs whose content matches the prepared query.</summary>
    IReadOnlyCollection<string> Run(CancellationToken cancellationToken);
}
