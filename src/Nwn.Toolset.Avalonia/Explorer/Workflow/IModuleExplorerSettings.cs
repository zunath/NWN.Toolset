using Nwn.Authoring.Resources;

namespace Nwn.Toolset.Avalonia.Explorer.Workflow;

/// <summary>Where the panel keeps the builder's preferences between sessions.</summary>
public interface IModuleExplorerSettings
{
    /// <summary>The tab that was showing, or null when nothing valid was saved.</summary>
    ModuleResourceType? SelectedSection { get; set; }
}
