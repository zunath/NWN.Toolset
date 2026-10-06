using Nwn.Toolset.Avalonia.Explorer.Workflow;

namespace Nwn.Toolset.Avalonia.Tests.Support;

internal sealed class FakeExplorerPreparedDeletion : IModuleExplorerPreparedDeletion
{
    private readonly Func<ModuleExplorerDeletionResult> _commit;

    public FakeExplorerPreparedDeletion(Func<ModuleExplorerDeletionResult> commit)
    {
        _commit = commit;
    }

    public ModuleExplorerDeletionResult Commit() => _commit();
}
