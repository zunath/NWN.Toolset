using Nwn.Authoring.Categories;
using Nwn.Authoring.Resources;

namespace Nwn.Toolset.Avalonia.Palettes.Workflow;

/// <summary>
/// The host's category sidecar. Where it lives, how it is seeded and locked, and how it detects external
/// changes are the host's business; the palette edits the live sections and then asks for a save.
/// </summary>
public interface IPaletteCategoryStore
{
    /// <summary>Raised after categories change, so the palette re-reads its tree.</summary>
    event Action? Changed;

    /// <summary>The live, writable Custom section for a type, or null when no module is open.</summary>
    CategorySection? Section(ModuleResourceType type);

    /// <summary>Whether <see cref="SaveChanges"/> would be refused, without writing or discarding anything.</summary>
    PaletteCategorySaveResult CanSaveChanges();

    /// <summary>
    /// Writes the pending edits. A refused or failed save must restore the last persisted tree, so the
    /// next <see cref="Section"/> call no longer carries the rejected edit.
    /// </summary>
    PaletteCategorySaveResult SaveChanges();
}
