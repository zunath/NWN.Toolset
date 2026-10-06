using System.Security.Cryptography;
using System.Text;

namespace Nwn.Authoring.Documents;

/// <summary>Filesystem-backed storage with sibling-temp writes and same-volume atomic replacement.
/// A named per-path mutex serializes cooperating writers across processes. The hash check and
/// replacement are not an atomic compare-and-swap against noncooperating external editors.</summary>
public sealed class FileDocumentStorage : IDocumentStorage
{
    public byte[]? ReadIfExists(string path) => File.Exists(path) ? File.ReadAllBytes(path) : null;

    public void WriteAtomically(string path, ReadOnlyMemory<byte> bytes, string? expectedCurrentHash)
    {
        var fullPath = System.IO.Path.GetFullPath(path);
        var directory = System.IO.Path.GetDirectoryName(fullPath)
            ?? throw new IOException($"Document path '{path}' has no parent directory.");
        Directory.CreateDirectory(directory);
        var temporaryPath = System.IO.Path.Combine(directory,
            $".{System.IO.Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
        var lockPath = OperatingSystem.IsWindows() ? fullPath.ToUpperInvariant() : fullPath;
        var lockName = "NwnAuthoringDocument_" + Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(lockPath)));
        using var pathMutex = new Mutex(initiallyOwned: false, name: lockName);
        var hasLock = false;

        try
        {
            using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write,
                       FileShare.None, 81920, FileOptions.WriteThrough))
            {
                stream.Write(bytes.Span);
                stream.Flush(flushToDisk: true);
            }

            try
            {
                hasLock = pathMutex.WaitOne(TimeSpan.FromSeconds(30));
            }
            catch (AbandonedMutexException)
            {
                hasLock = true;
            }
            if (!hasLock)
                throw new TimeoutException($"Timed out waiting to save '{fullPath}' because another authoring process holds its save lock.");

            var current = ReadIfExists(fullPath);
            var currentHash = current is null ? null : Convert.ToHexStringLower(SHA256.HashData(current));
            if (!string.Equals(currentHash, expectedCurrentHash, StringComparison.Ordinal))
                throw new DocumentConflictException(fullPath);

            if (expectedCurrentHash is null)
                File.Move(temporaryPath, fullPath);
            else
                File.Replace(temporaryPath, fullPath, destinationBackupFileName: null, ignoreMetadataErrors: true);
        }
        finally
        {
            if (hasLock)
                pathMutex.ReleaseMutex();
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }
}
