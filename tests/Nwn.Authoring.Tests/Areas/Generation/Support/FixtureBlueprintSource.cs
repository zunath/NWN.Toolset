using Nwn.Authoring.Areas.Generation.Hosting;
using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Resources;

namespace Nwn.Authoring.Tests.Areas.Generation.Support;

/// <summary>Serves blueprints for the fixture's object ResRefs and records which were requested.</summary>
internal sealed class FixtureBlueprintSource : IGeneratedAreaBlueprintSource
{
    private readonly HashSet<(ModuleResourceType Type, string ResRef)> _available = new();

    public List<(ModuleResourceType Type, string ResRef)> Requested { get; } = new();

    public float CreatureRadius { get; set; } = 0.5f;

    public FixtureBlueprintSource Add(ModuleResourceType type, string resRef)
    {
        _available.Add((type, resRef));
        return this;
    }

    public static FixtureBlueprintSource CreateComplete() => new FixtureBlueprintSource()
        .Add(ModuleResourceType.Utp, GeneratorFixture.PropResRef)
        .Add(ModuleResourceType.Utp, GeneratorFixture.ExitPlaceableResRef)
        .Add(ModuleResourceType.Utp, GeneratorFixture.TreasureResRef)
        .Add(ModuleResourceType.Utd, GeneratorFixture.ExitDoorResRef)
        .Add(ModuleResourceType.Utc, GeneratorFixture.CreatureResRef)
        .Add(ModuleResourceType.Utc, GeneratorFixture.BossResRef);

    public bool TryLoadBlueprint(ModuleResourceType type, string resRef, out JsonGffDocument blueprint)
    {
        Requested.Add((type, resRef));
        if (!_available.Contains((type, resRef)))
        {
            blueprint = null!;
            return false;
        }

        var root = new JsonGffStruct();
        root.SetString("Tag", GffFieldType.CExoString, resRef);
        blueprint = new JsonGffDocument("GFF ", root);
        return true;
    }

    public float GetCreatureCollisionRadius(string resRef, JsonGffDocument blueprint) => CreatureRadius;
}
