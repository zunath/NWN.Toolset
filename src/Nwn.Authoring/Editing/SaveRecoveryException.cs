namespace Nwn.Authoring.Editing;

/// <summary>Indicates that an interrupted grouped save could not be fully restored.</summary>
public sealed class SaveRecoveryException(IReadOnlyList<string> incompleteTransactions)
    : IOException(
        "Interrupted save recovery is incomplete for: " +
        string.Join("; ", incompleteTransactions) +
        ". Resolve the lock or restore these files manually, then reopen the module.")
{
    public IReadOnlyList<string> IncompleteTransactions { get; } = incompleteTransactions;
}
