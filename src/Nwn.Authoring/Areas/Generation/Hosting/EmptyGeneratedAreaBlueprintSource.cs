using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Resources;

namespace Nwn.Authoring.Areas.Generation.Hosting;

/// <summary>A blueprint source with no blueprints, for hosts whose generated areas place no objects.</summary>
public sealed class EmptyGeneratedAreaBlueprintSource : IGeneratedAreaBlueprintSource
{
    public static EmptyGeneratedAreaBlueprintSource Instance { get; } = new();

    private EmptyGeneratedAreaBlueprintSource()
    {
    }

    public bool TryLoadBlueprint(ModuleResourceType type, string resRef, out JsonGffDocument blueprint)
    {
        blueprint = null!;
        return false;
    }

    public float GetCreatureCollisionRadius(string resRef, JsonGffDocument blueprint) =>
        throw new InvalidOperationException($"No blueprint source is available for creature '{resRef}'.");
}
