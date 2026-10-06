using Nwn.Authoring.Areas.Generation.Composition;
using Nwn.Authoring.Areas.Generation.Hosting;
using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Documents.NimGff;

namespace Nwn.Authoring.Tests.Areas.Generation.Support;

/// <summary>Counts the instances the generator hands to the host's population policy.</summary>
internal sealed class RecordingPopulationPolicy : IGeneratedAreaPopulationPolicy
{
    public int ExitPlaceables { get; private set; }

    public int Treasures { get; private set; }

    public int Creatures { get; private set; }

    public DungeonTierDetail? TreasureTier { get; private set; }

    public void ConfigureExitPlaceable(JsonGffStruct instance) => ExitPlaceables++;

    public void ConfigureTreasure(JsonGffStruct instance, DungeonTierDetail tier)
    {
        Treasures++;
        TreasureTier = tier;
        instance.SetString("OnOpen", GffFieldType.ResRef, "fixture_open");
    }

    public void ConfigureCreature(JsonGffStruct instance) => Creatures++;
}
