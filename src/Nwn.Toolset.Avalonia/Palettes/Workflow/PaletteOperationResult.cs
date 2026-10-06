namespace Nwn.Toolset.Avalonia.Palettes.Workflow;

/// <summary>Whether a host operation happened, and the host's explanation when it did not.</summary>
public readonly record struct PaletteOperationResult(bool Succeeded, string? Problem)
{
    public static PaletteOperationResult Ok() => new(true, null);

    public static PaletteOperationResult Failed(string problem) => new(false, problem);
}
