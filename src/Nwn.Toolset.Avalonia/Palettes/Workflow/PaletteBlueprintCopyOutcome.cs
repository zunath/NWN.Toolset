namespace Nwn.Toolset.Avalonia.Palettes.Workflow;

/// <summary>How an Edit Copy request ended.</summary>
public enum PaletteBlueprintCopyOutcome
{
    /// <summary>The host wrote a module copy; the palette files it, reveals it and opens its editor.</summary>
    Copied,

    /// <summary>
    /// The host handed the source to an editor that makes the copy itself (for example a definition
    /// editor's copy mode, which chooses the new key). Nothing was written yet, so the palette neither
    /// files nor reveals anything and opens no editor of its own.
    /// </summary>
    OpenedInEditor,

    /// <summary>Nothing was copied; the host's explanation is in the problem.</summary>
    Failed
}
