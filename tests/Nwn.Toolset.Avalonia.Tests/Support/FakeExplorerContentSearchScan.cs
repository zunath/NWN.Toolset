using Nwn.Toolset.Avalonia.Explorer.Workflow;

namespace Nwn.Toolset.Avalonia.Tests.Support;

internal sealed class FakeExplorerContentSearchScan : IModuleExplorerContentSearchScan
{
    private readonly FakeExplorerContentSearch _owner;
    private readonly IReadOnlyDictionary<string, string> _snapshot;
    private readonly string _query;

    public FakeExplorerContentSearchScan(
        FakeExplorerContentSearch owner, IReadOnlyDictionary<string, string> snapshot, string query)
    {
        _owner = owner;
        _snapshot = snapshot;
        _query = query;
    }

    public IReadOnlyCollection<string> Run(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _owner.RecordRun();
        if (_owner.Failure is { } failure)
            throw new InvalidOperationException(failure);

        return _snapshot
            .Where(pair => pair.Value.Contains(_query, StringComparison.OrdinalIgnoreCase))
            .Select(pair => pair.Key)
            .ToArray();
    }
}
