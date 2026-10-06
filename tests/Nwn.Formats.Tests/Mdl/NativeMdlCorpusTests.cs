using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Formats.Mdl;

namespace Nwn.Formats.Tests.Mdl;

[TestClass]
public sealed class NativeMdlCorpusTests
{
    [TestMethod]
    [DataRow("pmh0", 56, 329, "1ECD5849CB8F08D920DC17C18CFD2DB63F6E8BB37B184FF41ED4884364B1959F")]
    [DataRow("pfh0", 57, 446, "82FFD41C6233CE400F11819CAE242B672EEC2590A091D72AB1720D8D37FF9C9B")]
    [DataRow("pmx0", 56, 221, "2D2AA38C4DDE726CFA2769848F0296DB51E4510F73E2597AA5954EA94514CB79")]
    [DataRow("pfx0", 56, 221, "9CBBB3EFEA3051E2166D5CA1E18C5A9E246562D54924A2CC18EEC0244DC672E2")]
    public void XenomechSkeletonsRetainTheirLocalHierarchyWithinInheritedNodeCounts(string name, int nodes, int declared, string sha256)
    {
        var root = Environment.GetEnvironmentVariable("XENOMECH_TEST_CONTENT_ROOT");
        Assert.IsFalse(string.IsNullOrWhiteSpace(root), "Select the read-only content corpus with XENOMECH_TEST_CONTENT_ROOT.");
        var bytes = File.ReadAllBytes(Path.Combine(root!, "xm_pt_root", name + ".mdl"));
        Assert.AreEqual(sha256, Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)));
        Assert.AreEqual(declared, System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(88, 4)));
        var scene = MdlBinaryReader.Read(bytes);
        Assert.AreEqual(name, scene.ModelName);
        Assert.AreEqual(nodes, scene.Nodes.Count);
        Assert.IsFalse(scene.HasAnimations, "The local static hierarchy contains no animation tracks; inherited clips require separate evaluation.");
        Assert.IsTrue(scene.Nodes.Any(node => node.Name == "torso_g"));
        Assert.IsTrue(scene.Nodes.Any(node => node.Name == "head_g"));
        foreach (var node in scene.Nodes.Where(node => node.Name is "torso_g" or "head_g" or "lbicep_g" or "rbicep_g"
            or "lforearm_g" or "rforearm_g" or "lhand_g" or "rhand_g"))
            Assert.AreEqual(System.Numerics.Vector4.Zero, node.Orientation, $"Native attachment {name}/{node.Name} is unrotated at rest.");
    }

    [TestMethod]
    public void XenomechCompiledRigidPartRetainsItsExplicitFourthBindingMaterial()
    {
        var root = Environment.GetEnvironmentVariable("XENOMECH_TEST_CONTENT_ROOT");
        Assert.IsFalse(string.IsNullOrWhiteSpace(root), "Select the read-only content corpus with XENOMECH_TEST_CONTENT_ROOT.");
        var path = Path.Combine(root!, "xm_pt_chest", "pmt0_chest240.mdl");
        var scene = MdlBinaryReader.Read(File.ReadAllBytes(path));
        var mesh = scene.Nodes.Single(node => node.Mesh is not null).Mesh!;
        Assert.IsNull(mesh.BitmapName);
        Assert.AreEqual("pmt0_chest240", mesh.MaterialName);
        Assert.IsTrue(mesh.Faces.Count > 100);
    }

    [TestMethod]
    public void SwlorAsciiModelSource_PreservesStaticMeshAndReportsAnimationTracks()
    {
        var root = Environment.GetEnvironmentVariable("SWLOR_TEST_HAKS_ROOT");
        if (string.IsNullOrWhiteSpace(root))
            Assert.Inconclusive("Set SWLOR_TEST_HAKS_ROOT to run the read-only native MDL check.");
        var path = Path.Combine(root!, "model_sources", "sw_cr_creature", "a_ba_non_combat.mdl.ascii");
        if (!File.Exists(path))
            Assert.Inconclusive($"Native ASCII MDL source is unavailable: {path}");

        var scene = MdlAsciiReader.Read(File.ReadAllBytes(path));
        Assert.AreEqual("a_ba_non_combat", scene.ModelName);
        Assert.IsTrue(scene.HasAnimations);
        Assert.IsTrue(scene.Nodes.Any(node => node.Name.Equals("torso_g", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(scene.Nodes.Where(node => node.Mesh is not null).All(node => node.Mesh!.Faces.Count > 0));
    }

    [TestMethod]
    public void SwlorCompiledArmorFixture_IsRejectedByAsciiOnlyReader()
    {
        var root = Environment.GetEnvironmentVariable("SWLOR_TEST_HAKS_ROOT");
        if (string.IsNullOrWhiteSpace(root))
            Assert.Inconclusive("Set SWLOR_TEST_HAKS_ROOT to run the read-only native MDL check.");
        var path = Path.Combine(root!, "sw_pt_chest", "pfa0_chest109.mdl");
        if (!File.Exists(path))
            Assert.Inconclusive($"Native compiled armor MDL is unavailable: {path}");

        var error = Assert.ThrowsExactly<NotSupportedException>(() => MdlAsciiReader.Read(File.ReadAllBytes(path)));
        StringAssert.Contains(error.Message, "Compiled binary MDL");
    }
}
