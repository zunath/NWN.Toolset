using Nwn.Authoring.Resources;
using Nwn.Toolset.Avalonia.Explorer;
using Nwn.Toolset.Avalonia.Explorer.Workflow;

namespace Nwn.Toolset.Avalonia.Tests.Support;

internal sealed class FakeExplorerSelection : IModuleExplorerSelection
{
    public List<(ModuleResourceType Type, ExplorerItem Item)> Selections { get; } = new();

    public void Selected(ModuleResourceType type, ExplorerItem item) => Selections.Add((type, item));
}
