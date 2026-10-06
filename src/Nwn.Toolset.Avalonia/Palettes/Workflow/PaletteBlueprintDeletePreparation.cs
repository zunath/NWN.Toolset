namespace Nwn.Toolset.Avalonia.Palettes.Workflow;

/// <summary>The result of <see cref="IPaletteBlueprintOperations.PrepareDelete"/>.</summary>
/// <param name="Deletion">The prepared deletion, or null when it was refused.</param>
/// <param name="Problem">Why the deletion was refused, as the status line shows it.</param>
/// <param name="LogDetail">The underlying reason, as the log records it.</param>
public sealed record PaletteBlueprintDeletePreparation(
    IPaletteBlueprintDeletion? Deletion,
    string? Problem,
    string? LogDetail)
{
    public static PaletteBlueprintDeletePreparation Ready(IPaletteBlueprintDeletion deletion) =>
        new(deletion ?? throw new ArgumentNullException(nameof(deletion)), null, null);

    public static PaletteBlueprintDeletePreparation Refused(string problem, string logDetail) =>
        new(null, problem, logDetail);
}
