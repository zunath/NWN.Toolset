using System.Security.Cryptography;
using System.Text.Json;
using Nwn.Authoring.Editing;

namespace Nwn.Authoring.Tests.Editing;

[TestClass]
public sealed class ScopedFileRecoveryTests
{
    [TestMethod]
    public void RecoveryRestoresSelectedCrossDirectoryGroupAndLeavesOtherTransactionsUnchanged()
    {
        using var directory = new TemporaryDirectory();
        var selected = new[] { directory.File("localization/strref.lock.json"), directory.File("preview/xenomech.tlk") };
        var own = CreateInterruptedGroup(directory.Path, selected, true);
        var foreign = CreateInterruptedGroup(directory.Path, [directory.File("other/area.are"), directory.File("other/area.git")], true);
        var foreignBackup = directory.File("other/orphan.tlk") + "." + Guid.NewGuid().ToString("N") + AtomicFileGroupWriter.BackupSuffix;
        File.WriteAllText(foreignBackup, "foreign orphan");
        var retained = foreign.Append(foreignBackup).ToDictionary(path => path, File.ReadAllBytes);
        var writer = new AtomicFileGroupWriter(new ExactTargetWriteAccess(selected));

        var restored = writer.RecoverInterruptedSaves(directory.Path, selected);

        CollectionAssert.AreEquivalent(selected, restored.ToArray());
        foreach (var target in selected) Assert.AreEqual("original " + target, File.ReadAllText(target));
        foreach (var path in own) Assert.IsFalse(File.Exists(path), path);
        foreach (var pair in retained) CollectionAssert.AreEqual(pair.Value, File.ReadAllBytes(pair.Key), pair.Key);
    }

    [TestMethod]
    public void RecoveryRemovesOnlySelectedNewMembersFromAnInterruptedGroup()
    {
        using var directory = new TemporaryDirectory();
        var targets = new[] { directory.File("localization/strref.lock.json"), directory.File("preview/xenomech.tlk") };
        var artifacts = CreateInterruptedGroup(directory.Path, targets, false);
        var writer = new AtomicFileGroupWriter(new ExactTargetWriteAccess(targets));

        writer.RecoverInterruptedSaves(directory.Path, targets);

        foreach (var target in targets) Assert.IsFalse(File.Exists(target));
        foreach (var path in artifacts) Assert.IsFalse(File.Exists(path));
    }

    [TestMethod]
    public void MixedScopeTransactionIsRefusedBeforeAnyMemberIsRestored()
    {
        using var directory = new TemporaryDirectory();
        var selected = directory.File("localization/strref.lock.json");
        var other = directory.File("other/area.are");
        var artifacts = CreateInterruptedGroup(directory.Path, [selected, other], true);
        var all = artifacts.Concat([selected, other]).ToDictionary(path => path, File.ReadAllBytes);
        var writer = new AtomicFileGroupWriter(new ExactTargetWriteAccess([selected]));

        Assert.ThrowsExactly<SaveRecoveryException>(() => writer.RecoverInterruptedSaves(directory.Path, [selected]));

        foreach (var pair in all) CollectionAssert.AreEqual(pair.Value, File.ReadAllBytes(pair.Key), pair.Key);
    }

    [TestMethod]
    public void RedirectedTemporaryPathIsRefusedWithoutDeletingAnotherFile()
    {
        using var directory = new TemporaryDirectory();
        var target = directory.File("preview/xenomech.tlk");
        var foreign = directory.File("other/keep.txt");
        File.WriteAllText(foreign, "keep");
        var artifacts = CreateInterruptedGroup(directory.Path, [target], true);
        var manifest = artifacts[0];
        using var json = JsonDocument.Parse(File.ReadAllBytes(manifest));
        var entry = json.RootElement.GetProperty("Entries")[0];
        File.WriteAllText(manifest, JsonSerializer.Serialize(new { Entries = new[] { new
        {
            TargetPath = target, TemporaryPath = foreign,
            BackupPath = entry.GetProperty("BackupPath").GetString(), HadOriginal = true,
            ReplacementSha256 = entry.GetProperty("ReplacementSha256").GetString()
        } } }));
        var all = artifacts.Concat([target, foreign]).ToDictionary(path => path, File.ReadAllBytes);
        var writer = new AtomicFileGroupWriter(new ExactTargetWriteAccess([target]));

        Assert.ThrowsExactly<SaveRecoveryException>(() => writer.RecoverInterruptedSaves(directory.Path, [target]));

        foreach (var pair in all) CollectionAssert.AreEqual(pair.Value, File.ReadAllBytes(pair.Key), pair.Key);
    }

    [TestMethod]
    public void CorruptManifestProtectsSelectedBackupAndRefusesSuccess()
    {
        using var directory = new TemporaryDirectory();
        var target = directory.File("preview/xenomech.tlk");
        var artifacts = CreateInterruptedGroup(directory.Path, [target], true);
        File.WriteAllText(artifacts[0], "{invalid");
        var all = artifacts.Concat([target]).ToDictionary(path => path, File.ReadAllBytes);
        var writer = new AtomicFileGroupWriter(new ExactTargetWriteAccess([target]));

        Assert.ThrowsExactly<SaveRecoveryException>(() => writer.RecoverInterruptedSaves(directory.Path, [target]));

        foreach (var pair in all) CollectionAssert.AreEqual(pair.Value, File.ReadAllBytes(pair.Key), pair.Key);
    }

    private static string[] CreateInterruptedGroup(string root, string[] targets, bool hadOriginal)
    {
        var transaction = Guid.NewGuid().ToString("N");
        var artifacts = new List<string> { Path.Combine(root, "." + transaction + AtomicFileGroupWriter.TransactionSuffix) };
        var entries = targets.Select(target =>
        {
            var temporary = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
            var backup = target + "." + transaction + AtomicFileGroupWriter.BackupSuffix;
            File.WriteAllText(target, "replacement " + target);
            File.WriteAllText(temporary, "unused staged bytes");
            artifacts.Add(temporary);
            if (hadOriginal)
            {
                File.WriteAllText(backup, "original " + target);
                artifacts.Add(backup);
            }
            return new { TargetPath = target, TemporaryPath = temporary, BackupPath = backup, HadOriginal = hadOriginal,
                ReplacementSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(target))) };
        }).ToArray();
        File.WriteAllText(artifacts[0], JsonSerializer.Serialize(new { Entries = entries }));
        return artifacts.ToArray();
    }

    private sealed class ExactTargetWriteAccess(string[] targets) : IFileWriteAccess
    {
        public void EnsureAllowed() { }
        public IDisposable Acquire(string path, TimeSpan? timeout = null)
        {
            Assert.IsTrue(targets.Contains(path, StringComparer.OrdinalIgnoreCase), "Recovery requested a broader write lease.");
            return new EmptyLease();
        }
    }

    private sealed class EmptyLease : IDisposable
    {
        public void Dispose() { }
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "scoped-recovery-" + Guid.NewGuid().ToString("N"));
        public TemporaryDirectory() => Directory.CreateDirectory(Path);
        public string File(string relative)
        {
            var result = System.IO.Path.Combine(Path, relative.Replace('/', System.IO.Path.DirectorySeparatorChar));
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(result)!);
            return result;
        }
        public void Dispose() => Directory.Delete(Path, true);
    }
}
