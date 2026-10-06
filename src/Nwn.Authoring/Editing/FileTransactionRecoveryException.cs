namespace Nwn.Authoring.Editing;

/// <summary>Raised when an interrupted file transaction cannot be safely restored.</summary>
public sealed class FileTransactionRecoveryException(string manifestPath, Exception innerException)
    : IOException(
        $"Could not recover interrupted file transaction '{manifestPath}': {innerException.Message}",
        innerException)
{
    public string ManifestPath { get; } = manifestPath;
}
