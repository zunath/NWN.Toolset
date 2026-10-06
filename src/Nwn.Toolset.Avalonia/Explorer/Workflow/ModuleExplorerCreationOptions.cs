namespace Nwn.Toolset.Avalonia.Explorer.Workflow;

/// <summary>What the host asked for after the name, such as which script template to start from.</summary>
/// <param name="TemplateId">The chosen template, or null when the type has no template choice.</param>
public sealed record ModuleExplorerCreationOptions(string? TemplateId)
{
    /// <summary>No further choice was needed.</summary>
    public static ModuleExplorerCreationOptions None { get; } = new((string?)null);
}
