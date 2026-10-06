namespace Nwn.Toolset.Avalonia.Palettes.Workflow;

/// <summary>The result of <see cref="IPaletteBlueprintOperations.CopyAsync"/>.</summary>
/// <param name="Outcome">How the request ended.</param>
/// <param name="ResRef">The new blueprint's resref. Null unless copied.</param>
/// <param name="Location">Where the copy was written, for the log. Null unless copied.</param>
/// <param name="Problem">The host's explanation when the copy failed.</param>
public sealed record PaletteBlueprintCopy(
    PaletteBlueprintCopyOutcome Outcome,
    string? ResRef,
    string? Location,
    string? Problem)
{
    /// <summary>True when a module copy was written.</summary>
    public bool Succeeded => Outcome == PaletteBlueprintCopyOutcome.Copied;

    public static PaletteBlueprintCopy Copied(string resRef, string location)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resRef);
        return new(PaletteBlueprintCopyOutcome.Copied, resRef, location, null);
    }

    /// <summary>The source was handed to an editor that makes the copy itself.</summary>
    public static PaletteBlueprintCopy OpenedInEditor() =>
        new(PaletteBlueprintCopyOutcome.OpenedInEditor, null, null, null);

    public static PaletteBlueprintCopy Failed(string problem) =>
        new(PaletteBlueprintCopyOutcome.Failed, null, null, problem);
}
