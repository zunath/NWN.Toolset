namespace Nwn.Toolset.Avalonia.Explorer.Workflow;

/// <summary>How a section's "New ..." action collects what it needs.</summary>
public enum ModuleExplorerCreationMode
{
    /// <summary>A name prompt, an optional host choice, then the host writes a template.</summary>
    NamePrompt,

    /// <summary>A nonmodal creation form shown over the tree, such as the new-area wizard.</summary>
    Form
}
