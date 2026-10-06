namespace Nwn.Authoring.Editing;

/// <summary>Describes a guarded replacement in a file transaction.</summary>
/// <remarks>
/// The expected bytes are checked when the transaction plan is captured. The transaction restores
/// those bytes if it must roll back, and accepts only the expected or replacement generation during
/// interrupted recovery.
/// </remarks>
public sealed class FileTransactionReplacement
{
    private readonly byte[] _expectedBytes;
    private readonly byte[] _replacementBytes;

    public FileTransactionReplacement(string path, byte[] expectedBytes, byte[] replacementBytes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(expectedBytes);
        ArgumentNullException.ThrowIfNull(replacementBytes);

        Path = path;
        _expectedBytes = expectedBytes.ToArray();
        _replacementBytes = replacementBytes.ToArray();
    }

    public string Path { get; }

    /// <summary>Returns a copy of the generation that must be present when the plan is captured.</summary>
    public byte[] ExpectedBytes => _expectedBytes.ToArray();

    /// <summary>Returns a copy of the bytes committed by the transaction.</summary>
    public byte[] ReplacementBytes => _replacementBytes.ToArray();

    internal byte[] ExpectedBytesUnsafe => _expectedBytes;

    internal byte[] ReplacementBytesUnsafe => _replacementBytes;
}
