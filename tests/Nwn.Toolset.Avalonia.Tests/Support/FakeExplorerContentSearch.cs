using Nwn.Authoring.Resources;
using Nwn.Toolset.Avalonia.Explorer.Workflow;

namespace Nwn.Toolset.Avalonia.Tests.Support;

/// <summary>Searches dialog "content" held in memory, recording which thread prepared and ran each scan.</summary>
internal sealed class FakeExplorerContentSearch : IModuleExplorerContentSearch
{
    public Dictionary<string, string> Content { get; } = new(StringComparer.OrdinalIgnoreCase);

    public List<int> PreparedOnThreads { get; } = new();

    public List<int> RanOnThreads { get; } = new();

    public int Prepares { get; private set; }

    public string? Failure { get; set; }

    public bool Supports(ModuleResourceType type) => type == ModuleResourceType.Dlg;

    public string SearchingLabel(ModuleResourceType type) => "Searching dialogue...";

    public string FailureMessage(ModuleResourceType type, Exception exception) => "Dialogue search failed: " + exception.Message;

    public IModuleExplorerContentSearchScan? Prepare(ModuleResourceType type, string query)
    {
        Prepares++;
        lock (PreparedOnThreads)
            PreparedOnThreads.Add(Environment.CurrentManagedThreadId);

        var snapshot = new Dictionary<string, string>(Content, StringComparer.OrdinalIgnoreCase);
        return new FakeExplorerContentSearchScan(this, snapshot, query);
    }

    internal void RecordRun()
    {
        lock (RanOnThreads)
            RanOnThreads.Add(Environment.CurrentManagedThreadId);
    }
}
