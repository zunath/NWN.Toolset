namespace Nwn.Authoring.Documents;

/// <summary>Bounds retained snapshot history so large documents cannot grow undo memory without
/// limit. When a limit is exceeded, the oldest undo snapshots are evicted first, then the oldest
/// redo snapshots.</summary>
public sealed record DocumentSessionOptions
{
    public int MaximumHistorySteps { get; init; } = 100;
    public long MaximumHistoryBytes { get; init; } = 32L * 1024 * 1024;

    internal void Validate()
    {
        if (MaximumHistorySteps < 0 || MaximumHistoryBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(DocumentSessionOptions), "History limits must be nonnegative.");
    }
}
