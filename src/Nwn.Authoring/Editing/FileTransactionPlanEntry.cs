namespace Nwn.Authoring.Editing;

internal sealed record FileTransactionPlanEntry(
    string Path,
    FileTransactionEntryKind Kind,
    bool Existed,
    string? OriginalSha256,
    byte[]? OriginalBytes,
    byte[]? ReplacementBytes);
