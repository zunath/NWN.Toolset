namespace Nwn.Toolset.Avalonia.Palettes.Workflow;

/// <summary>Whether a category sidecar write happened, and the host's explanation when it did not.</summary>
public readonly record struct PaletteCategorySaveResult(bool Saved, string? Problem)
{
    public static PaletteCategorySaveResult Ok() => new(true, null);

    public static PaletteCategorySaveResult Failed(string problem) => new(false, problem);
}
