using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Resources;

namespace Nwn.Authoring.Areas.Generation.Hosting;

/// <summary>The host's object blueprints, which generated placements are instantiated from.</summary>
public interface IGeneratedAreaBlueprintSource
{
    /// <summary>Loads a creature, door or placeable blueprint, or returns false when it is missing.</summary>
    bool TryLoadBlueprint(ModuleResourceType type, string resRef, out JsonGffDocument blueprint);

    /// <summary>The collision radius, in meters, that a placed creature needs clear around it.</summary>
    /// <exception cref="InvalidOperationException">The host cannot resolve a valid radius for the creature.</exception>
    float GetCreatureCollisionRadius(string resRef, JsonGffDocument blueprint);
}
