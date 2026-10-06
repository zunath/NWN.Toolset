using Nwn.Authoring.Resources;
using Nwn.Toolset.Avalonia.Areas;
using Nwn.Toolset.Avalonia.Palettes.Workflow;

namespace Nwn.Toolset.Avalonia.Explorer.Workflow;

/// <summary>
/// The host's resource creation. The panel captures the selected folder, asks for a name or shows the
/// host's form, files the result and opens it; the templates, paths and writes are the host's.
/// </summary>
public interface IModuleExplorerCreation
{
    /// <summary>Whether this type offers "New ...". Types that cannot be finished stay browsable only.</summary>
    bool CanCreate(ModuleResourceType type);

    ModuleExplorerCreationMode Mode(ModuleResourceType type);

    /// <summary>The resref a typed name becomes, or an empty string when it has no usable characters.</summary>
    string ToResRef(ModuleResourceType type, string name);

    /// <summary>Whether a resource of this type already uses the resref.</summary>
    bool Exists(ModuleResourceType type, string resRef);

    /// <summary>
    /// Asks any type-specific question after the name, such as a script template. Returns
    /// <see cref="ModuleExplorerCreationOptions.None"/> when there is nothing to ask, or null when the
    /// builder cancelled.
    /// </summary>
    Task<ModuleExplorerCreationOptions?> ChooseOptionsAsync(ModuleResourceType type);

    /// <summary>Writes the new resource; a failure carries the reason shown to the builder.</summary>
    PaletteOperationResult Create(
        ModuleResourceType type, string resRef, string name, ModuleExplorerCreationOptions options);

    /// <summary>The form for <see cref="ModuleExplorerCreationMode.Form"/> types, or null when none can open.</summary>
    IAreaCreationFormState? OpenForm(ModuleResourceType type, ModuleExplorerFormCallbacks callbacks);

    /// <summary>Called after a resource was created and filed, so the host can refresh its indexes.</summary>
    void Created(ModuleResourceType type, string resRef);
}
