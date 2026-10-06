namespace Nwn.Toolset.Avalonia.Palettes.Workflow;

/// <summary>How a blueprint creation request ended.</summary>
public enum PaletteBlueprintCreationOutcome
{
    Created,
    AlreadyExists,
    Failed,

    /// <summary>The builder closed the host's own creation UI without creating anything.</summary>
    Cancelled
}
