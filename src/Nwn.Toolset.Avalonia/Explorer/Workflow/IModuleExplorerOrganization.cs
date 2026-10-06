using Nwn.Authoring.Categories;
using Nwn.Authoring.Resources;

namespace Nwn.Toolset.Avalonia.Explorer.Workflow;

/// <summary>
/// The host's folder naming rules: the starting folders a never-organized section is given, and how a
/// row reads once its folders already say part of its name.
/// </summary>
public interface IModuleExplorerOrganization
{
    /// <summary>
    /// False while the names seeding depends on are still loading. Seeding is then retried on a later
    /// refresh instead of filing everything by bare resref and never trying again.
    /// </summary>
    bool IsReadyToSeed(ModuleResourceType type);

    /// <summary>Adds starting folders to an empty, never-seeded section. Returns how many were created.</summary>
    int Seed(CategorySection section, ModuleResourceType type, IReadOnlyList<ExplorerItem> items);

    /// <summary>
    /// What a resource row reads as inside a folder, or an empty string to show its usual text. An area
    /// named "Tatooine - Anchorhead - North Entrance" can read "North Entrance" under Tatooine/Anchorhead.
    /// </summary>
    string LeafLabel(ModuleResourceType type, ExplorerItem item);
}
