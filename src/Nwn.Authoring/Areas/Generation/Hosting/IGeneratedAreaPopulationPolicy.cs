using Nwn.Authoring.Areas.Generation.Composition;
using Nwn.Authoring.Documents.NimGff;

namespace Nwn.Authoring.Areas.Generation.Hosting;

/// <summary>
/// Game-specific adjustments applied to instances the generator places. The generator places and
/// grounds the objects; what makes one a loot container, a transition or a safe encounter creature is
/// the host's rule.
/// </summary>
public interface IGeneratedAreaPopulationPolicy
{
    /// <summary>Adjusts the placeable standing in for an area exit.</summary>
    void ConfigureExitPlaceable(JsonGffStruct instance);

    /// <summary>Adjusts the treasure container placed in the boss room for the chosen tier.</summary>
    void ConfigureTreasure(JsonGffStruct instance, DungeonTierDetail tier);

    /// <summary>Adjusts a creature instance placed for an encounter.</summary>
    void ConfigureCreature(JsonGffStruct instance);
}
