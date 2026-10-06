using Nwn.Authoring.Resources;
using Nwn.Toolset.Avalonia.Explorer;
using Nwn.Toolset.Avalonia.Explorer.Workflow;

namespace Nwn.Toolset.Avalonia.Tests.Support;

/// <summary>An in-memory module: areas, dialogs and scripts, each a list of named resources.</summary>
internal sealed class FakeExplorerContentSource : IModuleExplorerContentSource
{
    public event Action<ModuleResourceType, string>? ResourceChanged;

    public event Action? ContentChanged;

    public Dictionary<ModuleResourceType, List<ExplorerItem>> Resources { get; } = new()
    {
        [ModuleResourceType.Area] = new(),
        [ModuleResourceType.Dlg] = new(),
        [ModuleResourceType.Nss] = new()
    };

    public IReadOnlyList<ExplorerSection> Sections { get; set; } = new[]
    {
        new ExplorerSection(ModuleResourceType.Area, "Areas", "Area"),
        new ExplorerSection(ModuleResourceType.Dlg, "Dialogs", "Dialog"),
        new ExplorerSection(ModuleResourceType.Nss, "Scripts", "Script") { IsCompilable = true }
    };

    public bool IsModuleOpen { get; set; } = true;

    public void Add(ModuleResourceType type, string resRef, string? name = null) =>
        Resources[type].Add(new ExplorerItem(resRef, name, null));

    public bool Remove(ModuleResourceType type, string resRef) =>
        Resources[type].RemoveAll(item => item.ResRef.Equals(resRef, StringComparison.OrdinalIgnoreCase)) > 0;

    public IReadOnlyList<ExplorerItem> Items(ModuleResourceType type) =>
        IsModuleOpen && Resources.TryGetValue(type, out var items) ? items.ToList() : Array.Empty<ExplorerItem>();

    public int Count(ModuleResourceType type) => Items(type).Count;

    public bool Exists(ModuleResourceType type, string resRef) =>
        Items(type).Any(item => item.ResRef.Equals(resRef, StringComparison.OrdinalIgnoreCase));

    public void RaiseResourceChanged(ModuleResourceType type, string resRef) => ResourceChanged?.Invoke(type, resRef);

    public void RaiseContentChanged() => ContentChanged?.Invoke();
}
