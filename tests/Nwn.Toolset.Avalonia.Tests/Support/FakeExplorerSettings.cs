using Nwn.Authoring.Resources;
using Nwn.Toolset.Avalonia.Explorer.Workflow;

namespace Nwn.Toolset.Avalonia.Tests.Support;

internal sealed class FakeExplorerSettings : IModuleExplorerSettings
{
    public ModuleResourceType? SelectedSection { get; set; }
}
