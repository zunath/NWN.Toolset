using System.Security.Cryptography;
using System.Diagnostics;
using System.Text.Json;
using Nwn.Authoring.Editing;

namespace Nwn.Authoring.Tests.Editing;

[TestClass]
public sealed class FileTransactionTests
{
    [TestMethod]
    public void CommitRemovesCapturedFileAndReplacesGuardedCompanion()
    {
        using var directory = new TemporaryDirectory();
        var resource = directory.File("are", "area source");
        var ifo = directory.File("module.ifo", "old module index");
        var oldIfo = File.ReadAllBytes(ifo);
        var newIfo = "updated module index"u8.ToArray();
        var plan = FileTransactionPlan.Capture(
            directory.Path,
            [directory.Path],
            [resource],
            [new FileTransactionReplacement(ifo, oldIfo, newIfo)]);

        var result = FileTransaction.Commit(plan);

        Assert.IsFalse(File.Exists(resource));
        CollectionAssert.AreEqual(newIfo, File.ReadAllBytes(ifo));
        CollectionAssert.AreEqual(new[] { resource }, result.DeletedPaths.ToArray());
        Assert.AreEqual(0, result.CleanupWarnings.Count);
        Assert.AreEqual(0, Directory.GetFiles(directory.Path, ".*.file-transaction.json").Length);
    }

    [TestMethod]
    public void CommitRefusesAResourceGenerationChangedAfterPlanCapture()
    {
        using var directory = new TemporaryDirectory();
        var resource = directory.File("script.nss", "captured source");
        var plan = FileTransactionPlan.Capture(directory.Path, [directory.Path], [resource]);
        File.WriteAllText(resource, "newer source");

        Assert.ThrowsExactly<IOException>(() => FileTransaction.Commit(plan));

        Assert.AreEqual("newer source", File.ReadAllText(resource));
        Assert.AreEqual(0, Directory.GetFiles(directory.Path, ".*.file-transaction.json").Length);
    }

    [TestMethod]
    public void CommitRefusesAFileThatAppearsAtACapturedMissingTarget()
    {
        using var directory = new TemporaryDirectory();
        var primary = directory.File("build.nss", "captured source");
        var missingCompanion = Path.Combine(directory.Path, "build.ncs");
        var plan = FileTransactionPlan.Capture(directory.Path, [directory.Path], [primary, missingCompanion]);
        File.WriteAllText(missingCompanion, "newer compiled source");

        Assert.ThrowsExactly<IOException>(() => FileTransaction.Commit(plan));

        Assert.AreEqual("captured source", File.ReadAllText(primary));
        Assert.AreEqual("newer compiled source", File.ReadAllText(missingCompanion));
        Assert.AreEqual(0, Directory.GetFiles(directory.Path, ".*.file-transaction.json").Length);
    }

    [TestMethod]
    public void RecoveryRollsBackPartiallyAppliedDeleteAndReplacementWithoutChangingUntouchedGeneration()
    {
        using var directory = new TemporaryDirectory();
        var movedResource = Path.Combine(directory.Path, "first.nss");
        var untouchedResource = directory.File("second.nss", "second original");
        var replacementPath = directory.File("module.ifo", "updated module index");
        var movedOriginal = "first original"u8.ToArray();
        var replacementOriginal = "old module index"u8.ToArray();
        var replacementBytes = File.ReadAllBytes(replacementPath);
        var transactionId = Guid.NewGuid().ToString("N");
        var backupPath = movedResource + "." + transactionId + ".file-delete-backup";
        File.WriteAllBytes(backupPath, movedOriginal);
        var manifestPath = WriteInterruptedManifest(
            directory.Path,
            transactionId,
            [
                DeleteEntry(movedResource, backupPath, movedOriginal),
                DeleteEntry(untouchedResource, untouchedResource + "." + transactionId + ".file-delete-backup", "second original"u8.ToArray()),
                ReplaceEntry(replacementPath, replacementOriginal, replacementBytes)
            ]);

        var restored = FileTransaction.RecoverInterrupted(directory.Path, [directory.Path]);

        CollectionAssert.AreEqual(movedOriginal, File.ReadAllBytes(movedResource));
        Assert.AreEqual("second original", File.ReadAllText(untouchedResource));
        CollectionAssert.AreEqual(replacementOriginal, File.ReadAllBytes(replacementPath));
        Assert.IsFalse(File.Exists(backupPath));
        Assert.IsFalse(File.Exists(manifestPath));
        CollectionAssert.AreEquivalent(new[] { movedResource, replacementPath }, restored.ToArray());
    }

    [TestMethod]
    public void RecoveryRefusesTamperedBackupAndPreservesManifestEvidence()
    {
        using var directory = new TemporaryDirectory();
        var resource = Path.Combine(directory.Path, "area.are");
        var expected = "captured generation"u8.ToArray();
        var transactionId = Guid.NewGuid().ToString("N");
        var backupPath = resource + "." + transactionId + ".file-delete-backup";
        File.WriteAllText(backupPath, "tampered generation");
        var manifestPath = WriteInterruptedManifest(
            directory.Path,
            transactionId,
            [DeleteEntry(resource, backupPath, expected)]);

        Assert.ThrowsExactly<FileTransactionRecoveryException>(
            () => FileTransaction.RecoverInterrupted(directory.Path, [directory.Path]));

        Assert.IsTrue(File.Exists(backupPath));
        Assert.IsTrue(File.Exists(manifestPath));
        Assert.IsFalse(File.Exists(resource));
    }

    [TestMethod]
    public void RecoveryRefusesANewerTargetGenerationAndPreservesIt()
    {
        using var directory = new TemporaryDirectory();
        var resource = directory.File("area.are", "newer generation");
        var transactionId = Guid.NewGuid().ToString("N");
        var manifestPath = WriteInterruptedManifest(
            directory.Path,
            transactionId,
            [DeleteEntry(resource, resource + "." + transactionId + ".file-delete-backup", "captured generation"u8.ToArray())]);

        Assert.ThrowsExactly<FileTransactionRecoveryException>(
            () => FileTransaction.RecoverInterrupted(directory.Path, [directory.Path]));

        Assert.AreEqual("newer generation", File.ReadAllText(resource));
        Assert.IsTrue(File.Exists(manifestPath));
    }

    [TestMethod]
    public void RecoveryKeepsPartialDeletePendingWhenReplacementHasANewerGeneration()
    {
        using var directory = new TemporaryDirectory();
        var resource = Path.Combine(directory.Path, "area.are");
        var originalResource = "captured area"u8.ToArray();
        var transactionId = Guid.NewGuid().ToString("N");
        var backupPath = resource + "." + transactionId + ".file-delete-backup";
        File.WriteAllBytes(backupPath, originalResource);
        var replacementPath = directory.File("module.ifo", "unrecognized newer generation");
        var manifestPath = WriteInterruptedManifest(
            directory.Path,
            transactionId,
            [
                DeleteEntry(resource, backupPath, originalResource),
                ReplaceEntry(replacementPath, "old module index"u8.ToArray(), "updated module index"u8.ToArray())
            ]);

        Assert.ThrowsExactly<FileTransactionRecoveryException>(
            () => FileTransaction.RecoverInterrupted(directory.Path, [directory.Path]));

        Assert.IsFalse(File.Exists(resource));
        Assert.IsTrue(File.Exists(backupPath));
        Assert.AreEqual("unrecognized newer generation", File.ReadAllText(replacementPath));
        Assert.IsTrue(File.Exists(manifestPath));
    }

    [TestMethod]
    public void CaptureRefusesReparsePointMutationAndJournalPaths()
    {
        using var directory = new TemporaryDirectory();
        using var outside = new TemporaryDirectory();
        var target = outside.File("outside.nss", "outside source");
        if (!OperatingSystem.IsWindows())
        {
            var linkedFile = Path.Combine(directory.Path, "linked.nss");
            File.CreateSymbolicLink(linkedFile, target);
            try
            {
                Assert.ThrowsExactly<InvalidDataException>(() =>
                    FileTransactionPlan.Capture(directory.Path, [directory.Path], [linkedFile]));
            }
            finally
            {
                File.Delete(linkedFile);
            }
        }

        var linkedDirectory = Path.Combine(directory.Path, "linked-journal");
        CreateDirectoryLink(linkedDirectory, outside.Path);
        try
        {
            Assert.ThrowsExactly<InvalidDataException>(() =>
                FileTransactionPlan.Capture(linkedDirectory, [directory.Path], [target]));
            var linkedTarget = Path.Combine(linkedDirectory, Path.GetFileName(target));
            Assert.ThrowsExactly<InvalidDataException>(() =>
                FileTransactionPlan.Capture(directory.Path, [directory.Path], [linkedTarget]));
        }
        finally
        {
            Directory.Delete(linkedDirectory, recursive: false);
        }
    }

    [TestMethod]
    public void CaptureRejectsDuplicateAndOutOfRootMutationTargets()
    {
        using var directory = new TemporaryDirectory();
        using var outside = new TemporaryDirectory();
        var resource = directory.File("area.are", "area source");
        var outsideFile = outside.File("outside.nss", "outside source");

        Assert.ThrowsExactly<ArgumentException>(() =>
            FileTransactionPlan.Capture(directory.Path, [directory.Path], [resource, resource]));
        Assert.ThrowsExactly<ArgumentException>(() =>
            FileTransactionPlan.Capture(directory.Path, [directory.Path], [outsideFile]));
    }

    private static object DeleteEntry(string path, string backupPath, byte[] originalBytes) => new
    {
        Kind = 0,
        Path = path,
        BackupPath = backupPath,
        OriginalSha256 = Hash(originalBytes),
        OriginalBytesBase64 = (string?)null,
        ReplacementSha256 = (string?)null,
        ReplacementBytesBase64 = (string?)null
    };

    private static object ReplaceEntry(string path, byte[] originalBytes, byte[] replacementBytes) => new
    {
        Kind = 1,
        Path = path,
        BackupPath = (string?)null,
        OriginalSha256 = Hash(originalBytes),
        OriginalBytesBase64 = Convert.ToBase64String(originalBytes),
        ReplacementSha256 = Hash(replacementBytes),
        ReplacementBytesBase64 = Convert.ToBase64String(replacementBytes)
    };

    private static string WriteInterruptedManifest(string root, string transactionId, object[] entries)
    {
        var manifestPath = Path.Combine(root, "." + transactionId + ".file-transaction.json");
        var manifest = new
        {
            Version = 1,
            TransactionId = transactionId,
            TransactionRoot = root,
            AllowedRoots = new[] { root },
            Entries = entries
        };
        File.WriteAllBytes(manifestPath, JsonSerializer.SerializeToUtf8Bytes(manifest));
        return manifestPath;
    }

    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));

    private static void CreateDirectoryLink(string linkPath, string targetPath)
    {
        if (!OperatingSystem.IsWindows())
        {
            Directory.CreateSymbolicLink(linkPath, targetPath);
            return;
        }

        using var process = Process.Start(new ProcessStartInfo("cmd.exe", $"/c mklink /J \"{linkPath}\" \"{targetPath}\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        }) ?? throw new InvalidOperationException("Could not create the directory junction fixture.");
        process.WaitForExit();
        if (process.ExitCode != 0 || !Directory.Exists(linkPath))
            throw new InvalidOperationException("Could not create the directory junction fixture.");
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "file-transaction-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public string File(string name, string contents)
        {
            var path = System.IO.Path.Combine(Path, name);
            System.IO.File.WriteAllText(path, contents);
            return path;
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
