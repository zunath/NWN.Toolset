using Nwn.Authoring.Categories;
using Nwn.Authoring.Resources;
using Nwn.Toolset.Avalonia.Palettes.Workflow;

namespace Nwn.Toolset.Avalonia.Tests.Support;

internal sealed class FakePaletteContentSource : IPaletteContentSource
{
    public Dictionary<ModuleResourceType, HashSet<string>> Custom { get; } = new();

    public Dictionary<(ModuleResourceType, string), string> Names { get; } = new();

    public Dictionary<ModuleResourceType, StandardPalette> StandardPalettes { get; } = new();

    public IReadOnlyList<ModuleResourceType> OfferedTypes { get; set; } =
        new[] { ModuleResourceType.Utc, ModuleResourceType.Utp };

    public bool IsModuleOpen { get; set; } = true;

    public void AddCustom(ModuleResourceType type, string resRef, string? name = null)
    {
        if (!Custom.TryGetValue(type, out var resRefs))
            Custom[type] = resRefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        resRefs.Add(resRef);
        if (name is not null)
            Names[(type, resRef)] = name;
    }

    public IReadOnlySet<string> CustomResRefs(ModuleResourceType type) =>
        Custom.TryGetValue(type, out var resRefs)
            ? new HashSet<string>(resRefs, StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public string? CustomName(ModuleResourceType type, string resRef) =>
        Names.TryGetValue((type, resRef), out var name) ? name : null;

    public StandardPalette Standard(ModuleResourceType type) =>
        StandardPalettes.TryGetValue(type, out var palette) ? palette : StandardPalette.Empty;

    public string PluralName(ModuleResourceType type) => type == ModuleResourceType.Utc ? "Creatures" : "Placeables";

    public string SingularName(ModuleResourceType type) => type == ModuleResourceType.Utc ? "Creature" : "Placeable";
}
