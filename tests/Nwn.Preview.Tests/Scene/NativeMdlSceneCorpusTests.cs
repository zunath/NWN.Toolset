using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Formats.Mdl;
using Nwn.Preview.Scene;

namespace Nwn.Preview.Tests.Scene;

[TestClass]
public sealed class NativeMdlSceneCorpusTests
{
    [TestMethod]
    public void SwlorAsciiModelSource_PreparesStaticGeometryAndReportsUnappliedAnimations()
    {
        var root = Environment.GetEnvironmentVariable("SWLOR_TEST_HAKS_ROOT");
        if (string.IsNullOrWhiteSpace(root))
            Assert.Inconclusive("Set SWLOR_TEST_HAKS_ROOT to run the read-only native model preview check.");
        var path = Path.Combine(root!, "model_sources", "sw_cr_creature", "a_ba_non_combat.mdl.ascii");
        if (!File.Exists(path))
            Assert.Inconclusive($"Native ASCII MDL source is unavailable: {path}");

        var source = MdlAsciiReader.Read(File.ReadAllBytes(path));
        var scene = MdlScenePreparer.Prepare(source);

        Assert.IsTrue(scene.HasAnimations);
        Assert.IsFalse(scene.AnimationTracksApplied);
        Assert.IsTrue(scene.Nodes.Any(node => node.Mesh is { Faces.Count: > 0 }));
        Assert.IsTrue(scene.Nodes.Where(node => node.Mesh is not null)
            .SelectMany(node => node.Mesh!.Vertices).All(vertex => float.IsFinite(vertex.X) && float.IsFinite(vertex.Y) && float.IsFinite(vertex.Z)));
    }
}
