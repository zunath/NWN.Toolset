using Nwn.Authoring.Resources;

namespace Nwn.Toolset.Avalonia.Areas.Properties;

/// <summary>One placed-instance list of an area's GIT and the caption of its section.</summary>
/// <param name="Type">The blueprint type the list holds.</param>
/// <param name="ListFieldName">The GIT list field, as in "Creature List".</param>
/// <param name="Title">The section caption.</param>
public sealed record AreaInstanceSectionDefinition(
    ModuleResourceType Type,
    string ListFieldName,
    AreaPropertiesStringId Title);
