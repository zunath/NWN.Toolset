using Nwn.Toolset.Avalonia.Palettes.Workflow;

namespace Nwn.Toolset.Avalonia.Explorer.Workflow;

/// <summary>
/// Everything a host supplies to <see cref="ModuleExplorerController"/>. Content and categories are
/// required; every other service is optional and the panel offers less without it: no prompts means no
/// create, folder or delete commands that ask a question; no editors means nothing opens; no creation or
/// deletion means no New or Delete; no organization means no seeded folders.
/// </summary>
/// <remarks>
/// Prompts, the category sidecar, the write gate and the log are the palette's interfaces: both panels
/// work on the same sidecar under the same module lock, so a host implements each once.
/// </remarks>
/// <param name="Content">The host's sections and resources.</param>
/// <param name="Categories">The host's category sidecar.</param>
public sealed record ModuleExplorerHost(IModuleExplorerContentSource Content, IPaletteCategoryStore Categories)
{
    /// <summary>Starting folders and in-folder row labels.</summary>
    public IModuleExplorerOrganization? Organization { get; init; }

    /// <summary>Full-text search over resource content.</summary>
    public IModuleExplorerContentSearch? Search { get; init; }

    /// <summary>"New ..." for each section.</summary>
    public IModuleExplorerCreation? Creation { get; init; }

    /// <summary>Opening, compiling and closing editors.</summary>
    public IModuleExplorerEditors? Editors { get; init; }

    /// <summary>Logical resource deletion.</summary>
    public IModuleExplorerDeletion? Deletion { get; init; }

    /// <summary>Selection forwarding.</summary>
    public IModuleExplorerSelection? Selection { get; init; }

    /// <summary>Text prompts and destructive confirmations.</summary>
    public IPalettePrompts? Prompts { get; init; }

    /// <summary>Persisted panel preferences.</summary>
    public IModuleExplorerSettings? Settings { get; init; }

    /// <summary>The module-wide write lock.</summary>
    public IPaletteWriteGate? WriteGate { get; init; }

    /// <summary>The host's output log.</summary>
    public IPaletteLog? Log { get; init; }

    /// <summary>Text for the panel and its messages, or null for English.</summary>
    public ModuleExplorerTexts? Texts { get; init; }
}
