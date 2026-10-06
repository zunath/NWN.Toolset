using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Resources;
using Nwn.Toolset.Avalonia.Areas.Properties;

namespace Nwn.Toolset.Avalonia.Tests.Support;

internal sealed class FakeAreaInstanceBlueprintSource : IAreaInstanceBlueprintSource
{
    public string ModuleIdentity { get; init; } = "module-a";

    public Dictionary<string, JsonGffDocument> Blueprints { get; } = new(StringComparer.OrdinalIgnoreCase);

    public List<(ModuleResourceType Type, string ResRef, bool Indexed)> Loads { get; } = new();

    public JsonGffDocument LoadBlueprint(ModuleResourceType type, string resRef, bool useIndexedBlueprint)
    {
        Loads.Add((type, resRef, useIndexedBlueprint));
        return Blueprints.TryGetValue(resRef, out var blueprint)
            ? blueprint
            : throw new FileNotFoundException($"No blueprint {resRef}.");
    }
}
