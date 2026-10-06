using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Formats.NativeModels;
using Nwn.Preview.Scene;

namespace Nwn.Preview.Tests.Scene;

[TestClass]
public sealed class ComposedPartTexturesTests
{
    [TestMethod]
    public void NullBitmapKeepsTheStampedBodyPartTexture()
    {
        var source = ModelWithMesh("Hand", "NULL");
        var composed = ModelWithMesh("Hand", "pmh0_handl001");
        var textures = new ComposedPartTextures();

        textures.Record("pmh0_handl001", source);
        textures.Restore(composed, _ => true);

        Assert.AreEqual("pmh0_handl001", composed.GetMeshNodes().Single().Bitmap,
            "NULL means the standard body-part texture convention, not a literal texture");
    }

    [TestMethod]
    public void RealAuthoredTextureStillReplacesTheStampedPartTexture()
    {
        var source = ModelWithMesh("arm", "n_repsold01");
        var composed = ModelWithMesh("arm", "pmh0_bicepl249");
        var textures = new ComposedPartTextures();

        textures.Record("pmh0_bicepl249", source);
        textures.Restore(composed, name => name.Equals("n_repsold01", StringComparison.OrdinalIgnoreCase));

        Assert.AreEqual("n_repsold01", composed.GetMeshNodes().Single().Bitmap);
    }

    private static MdlModel ModelWithMesh(string meshName, string bitmap)
    {
        var root = new MdlNode { Name = "root" };
        root.Children.Add(new MdlTrimeshNode
        {
            Name = meshName,
            Bitmap = bitmap,
            Parent = root
        });
        return new MdlModel { Name = "part", GeometryRoot = root };
    }
}
