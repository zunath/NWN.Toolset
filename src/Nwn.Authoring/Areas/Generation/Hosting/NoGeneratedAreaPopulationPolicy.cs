using Nwn.Authoring.Areas.Generation.Composition;
using Nwn.Authoring.Documents.NimGff;

namespace Nwn.Authoring.Areas.Generation.Hosting;

/// <summary>A population policy that leaves every placed instance exactly as its blueprint defines it.</summary>
public sealed class NoGeneratedAreaPopulationPolicy : IGeneratedAreaPopulationPolicy
{
    public static NoGeneratedAreaPopulationPolicy Instance { get; } = new();

    private NoGeneratedAreaPopulationPolicy()
    {
    }

    public void ConfigureExitPlaceable(JsonGffStruct instance)
    {
    }

    public void ConfigureTreasure(JsonGffStruct instance, DungeonTierDetail tier)
    {
    }

    public void ConfigureCreature(JsonGffStruct instance)
    {
    }
}
