namespace Nwn.Toolset.Avalonia.Palettes.Workflow;

/// <summary>
/// The result of <see cref="IPaletteBlueprintOperations.CreateAsync"/> or of a host's own
/// <see cref="IPaletteBlueprintCreationDialog.CreateAsync"/>.
/// </summary>
/// <param name="Outcome">How the request ended.</param>
/// <param name="Location">Where the blueprint was written, for the log. Null unless created.</param>
/// <param name="Problem">The host's explanation when the write failed.</param>
public sealed record PaletteBlueprintCreation(
    PaletteBlueprintCreationOutcome Outcome,
    string? Location,
    string? Problem)
{
    /// <summary>
    /// The created blueprint's resref when the host chose it in its own creation UI. Null when the palette
    /// chose it from the name the builder typed.
    /// </summary>
    public string? ResRef { get; init; }

    /// <summary>The created blueprint's display name when the host chose it, or null.</summary>
    public string? Name { get; init; }

    /// <summary>
    /// True when the host's creation UI already opened the new blueprint's editor, so the palette does not
    /// open a second one.
    /// </summary>
    public bool OpenedInEditor { get; init; }

    public static PaletteBlueprintCreation Created(string location) =>
        new(PaletteBlueprintCreationOutcome.Created, location, null);

    /// <summary>A blueprint the host's own creation UI wrote, naming it itself.</summary>
    public static PaletteBlueprintCreation CreatedByHost(
        string resRef,
        string? name,
        string location,
        bool openedInEditor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resRef);
        return new(PaletteBlueprintCreationOutcome.Created, location, null)
        {
            ResRef = resRef,
            Name = name,
            OpenedInEditor = openedInEditor
        };
    }

    public static PaletteBlueprintCreation AlreadyExists() =>
        new(PaletteBlueprintCreationOutcome.AlreadyExists, null, null);

    public static PaletteBlueprintCreation Failed(string problem) =>
        new(PaletteBlueprintCreationOutcome.Failed, null, problem);

    public static PaletteBlueprintCreation Cancelled() =>
        new(PaletteBlueprintCreationOutcome.Cancelled, null, null);
}
