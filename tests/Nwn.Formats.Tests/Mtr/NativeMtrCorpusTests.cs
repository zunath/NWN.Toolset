using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Formats.Mtr;

namespace Nwn.Formats.Tests.Mtr;

[TestClass]
public sealed class NativeMtrCorpusTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    [DataRow("XENOMECH_TEST_CONTENT_ROOT")]
    [DataRow("SWLOR_TEST_HAKS_ROOT")]
    public void EverySelectedSourceMaterialLoadsAndPreservesItsOriginalBytes(string rootVariable)
    {
        var root = Environment.GetEnvironmentVariable(rootVariable);
        Assert.IsFalse(string.IsNullOrWhiteSpace(root), $"Select the read-only material corpus with {rootVariable}.");
        Assert.IsTrue(Directory.Exists(root), $"The selected {rootVariable} corpus must exist.");
        var paths = Directory.GetFiles(root!, "*.mtr", SearchOption.AllDirectories).Order(StringComparer.Ordinal).ToArray();
        Assert.IsTrue(paths.Length > 100, "The full material source corpus is required.");
        var failures = new List<string>();
        var unrecognizedCount = 0;
        foreach (var path in paths)
        {
            try
            {
                var bytes = File.ReadAllBytes(path);
                var material = MtrReader.Read(bytes);
                CollectionAssert.AreEqual(bytes, material.CopySourceBytes());
                unrecognizedCount += material.UnrecognizedDirectives.Count + material.Parameters.Values.Count(parameter => parameter.Kind == MtrParameterKind.Uninterpreted);
            }
            catch (Exception exception) when (exception is FormatException or AssertFailedException)
            {
                failures.Add($"{Path.GetRelativePath(root!, path)}: {exception.Message}");
            }
        }
        TestContext.WriteLine($"{rootVariable}: {paths.Length} material files; {failures.Count} failures; {unrecognizedCount} uninterpreted directives retained.");
        Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures.Take(20)));
    }
}
