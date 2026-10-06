namespace Nwn.Authoring.Editing;

/// <summary>Describes a committed file transaction and backup-cleanup failures.</summary>
public sealed record FileTransactionResult(
    IReadOnlyList<string> DeletedPaths,
    IReadOnlyList<string> CleanupWarnings);
