using Nwn.Authoring.Resources;

namespace Nwn.Toolset.Avalonia.Areas.Properties;

/// <summary>The native GIT placement lists, in the order the Properties page shows them.</summary>
public static class AreaInstanceSectionCatalog
{
    public static IReadOnlyList<AreaInstanceSectionDefinition> All { get; } =
    [
        new(ModuleResourceType.Utc, "Creature List", AreaPropertiesStringId.SectionCreatures),
        new(ModuleResourceType.Utp, "Placeable List", AreaPropertiesStringId.SectionPlaceables),
        new(ModuleResourceType.Utd, "Door List", AreaPropertiesStringId.SectionDoors),
        new(ModuleResourceType.Utw, "WaypointList", AreaPropertiesStringId.SectionWaypoints),
        new(ModuleResourceType.Utm, "StoreList", AreaPropertiesStringId.SectionStores),
        new(ModuleResourceType.Uts, "SoundList", AreaPropertiesStringId.SectionSounds),
        new(ModuleResourceType.Utt, "TriggerList", AreaPropertiesStringId.SectionTriggers),
        // Loose items on the ground. The GIT calls this one just "List".
        new(ModuleResourceType.Uti, "List", AreaPropertiesStringId.SectionItems),
    ];
}
