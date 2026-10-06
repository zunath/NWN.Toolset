using Nwn.Authoring.Resources;

namespace Nwn.Toolset.Avalonia.Explorer.Workflow;

/// <summary>Told when a resource row is selected, so the host can show it elsewhere (e.g. Properties).</summary>
public interface IModuleExplorerSelection
{
    void Selected(ModuleResourceType type, ExplorerItem item);
}
