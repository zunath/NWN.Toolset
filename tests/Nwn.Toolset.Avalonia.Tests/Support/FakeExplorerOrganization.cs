using Nwn.Authoring.Categories;
using Nwn.Authoring.Resources;
using Nwn.Toolset.Avalonia.Explorer;
using Nwn.Toolset.Avalonia.Explorer.Workflow;

namespace Nwn.Toolset.Avalonia.Tests.Support;

/// <summary>Seeds one folder per "Planet - Place" prefix and shortens rows to the part after it.</summary>
internal sealed class FakeExplorerOrganization : IModuleExplorerOrganization
{
    private const string Separator = " - ";

    public bool Ready { get; set; } = true;

    public int SeedCalls { get; private set; }

    public bool IsReadyToSeed(ModuleResourceType type) => Ready;

    public int Seed(CategorySection section, ModuleResourceType type, IReadOnlyList<ExplorerItem> items)
    {
        SeedCalls++;
        var created = 0;
        foreach (var item in items)
        {
            var name = item.Name ?? string.Empty;
            var split = name.IndexOf(Separator, StringComparison.Ordinal);
            if (split <= 0)
                continue;

            var prefix = name[..split];
            var folder = section.Find(prefix);
            if (folder == null)
            {
                folder = section.AddFolder(prefix);
                created++;
            }

            folder.AddMember(item.ResRef);
        }

        return created;
    }

    public string LeafLabel(ModuleResourceType type, ExplorerItem item)
    {
        var name = item.Name ?? string.Empty;
        var split = name.IndexOf(Separator, StringComparison.Ordinal);
        return split > 0 ? name[(split + Separator.Length)..] : string.Empty;
    }
}
