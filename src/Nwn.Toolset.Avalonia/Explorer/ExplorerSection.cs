using Nwn.Authoring.Resources;

namespace Nwn.Toolset.Avalonia.Explorer;

/// <summary>One tab of Module Contents: a resource type the host lists, and what it is called.</summary>
/// <param name="Type">The resource type, which is also the category sidecar section the tab organizes.</param>
/// <param name="Label">The plural name the tab shows, e.g. "Scripts".</param>
/// <param name="SingularLabel">The singular name "New ..." and status lines use, e.g. "Script".</param>
public sealed record ExplorerSection(ModuleResourceType Type, string Label, string SingularLabel)
{
    /// <summary>
    /// True for the one kind with a build step (scripts): the tab offers Compile, and a newly created
    /// resource is reported as needing compilation before the game runs it.
    /// </summary>
    public bool IsCompilable { get; init; }
}
