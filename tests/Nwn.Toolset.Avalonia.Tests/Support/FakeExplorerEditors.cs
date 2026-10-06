using Nwn.Authoring.Resources;
using Nwn.Toolset.Avalonia.Explorer.Workflow;

namespace Nwn.Toolset.Avalonia.Tests.Support;

internal sealed class FakeExplorerEditors : IModuleExplorerEditors
{
    public HashSet<(ModuleResourceType, string)> OpenEditors { get; } = new();

    public List<(ModuleResourceType Type, string ResRef)> Opened { get; } = new();

    public List<(ModuleResourceType Type, string ResRef)> Compiled { get; } = new();

    public List<(ModuleResourceType Type, string ResRef)> Closed { get; } = new();

    public bool IsModulePropertiesOpen { get; set; }

    public bool CanOpen(ModuleResourceType type) => true;

    public void Open(ModuleResourceType type, string resRef) => Opened.Add((type, resRef));

    public bool IsOpen(ModuleResourceType type, string resRef) => OpenEditors.Contains((type, resRef));

    public bool TryCloseForDeletion(ModuleResourceType type, string resRef)
    {
        Closed.Add((type, resRef));
        return OpenEditors.Remove((type, resRef));
    }

    public Task CompileAsync(ModuleResourceType type, string resRef)
    {
        Compiled.Add((type, resRef));
        return Task.CompletedTask;
    }
}
