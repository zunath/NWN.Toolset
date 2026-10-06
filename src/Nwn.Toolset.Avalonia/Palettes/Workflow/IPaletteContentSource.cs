using Nwn.Authoring.Categories;
using Nwn.Authoring.Resources;

namespace Nwn.Toolset.Avalonia.Palettes.Workflow;

/// <summary>
/// The blueprints a host offers in its palette: which types, which resrefs exist on each side, and
/// what they are called.
/// </summary>
public interface IPaletteContentSource
{
    /// <summary>The blueprint types the type row offers, in the order it shows them. Tiles always lead.</summary>
    IReadOnlyList<ModuleResourceType> OfferedTypes { get; }

    /// <summary>Whether a module is open, so blueprints can be created, copied or deleted.</summary>
    bool IsModuleOpen { get; }

    /// <summary>Every resref of a type the module itself owns, for counts and Unsorted.</summary>
    IReadOnlySet<string> CustomResRefs(ModuleResourceType type);

    /// <summary>A module blueprint's display name, or null to show its resref.</summary>
    string? CustomName(ModuleResourceType type, string resRef);

    /// <summary>
    /// The base game's palette for a type. Never null: an empty palette when the host has no base game
    /// or the game ships none for this type.
    /// </summary>
    StandardPalette Standard(ModuleResourceType type);

    /// <summary>The plural name of a type, as the type row and status messages show it.</summary>
    string PluralName(ModuleResourceType type);

    /// <summary>The singular name of a type, as "New ..." and delete prompts show it.</summary>
    string SingularName(ModuleResourceType type);
}
