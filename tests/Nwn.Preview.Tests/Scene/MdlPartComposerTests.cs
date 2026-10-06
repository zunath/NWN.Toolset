using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Formats.NativeModels;
using Nwn.Preview.Scene;

namespace Nwn.Preview.Tests.Scene;

[TestClass]
public sealed class MdlPartComposerTests
{
    [TestMethod]
    public void ComposeStampedClonesSkeletonAndAttachesStampedPartToMappedBone()
    {
        var torso = new MdlNode { Name = "torso_g" };
        var skeletonRoot = new MdlNode { Name = "root" };
        skeletonRoot.Children.Add(torso);
        torso.Parent = skeletonRoot;
        var skeleton = new MdlModel { Name = "body", GeometryRoot = skeletonRoot };

        var partMesh = new MdlTrimeshNode
        {
            Name = "torso_g",
            Bitmap = "authored_texture",
            Vertices = [Vector3.Zero, Vector3.UnitX, Vector3.UnitZ]
        };
        var partRoot = new MdlNode { Name = "part_root" };
        partRoot.Children.Add(partMesh);
        var part = new MdlModel { Name = "chest_part", GeometryRoot = partRoot };

        MdlModel? Load(string resRef, bool withSupermodelAnimations) =>
            (resRef, withSupermodelAnimations) switch
            {
                ("body", true) => skeleton,
                ("chest_part", false) => part,
                _ => null
            };

        var result = new MdlPartComposer(Load).ComposeStamped(
            "body", [("chest", "chest_part", "attachment-0")]);

        Assert.IsNotNull(result);
        Assert.AreNotSame(skeleton, result);
        Assert.AreEqual("authored_texture", partMesh.Bitmap);
        var attachedRoot = result.GeometryRoot!.Children.Single(node => node.Name == "torso_g");
        Assert.AreSame(result.GeometryRoot, attachedRoot.Parent);
        var attachedPart = attachedRoot.Children.Single(node => node.Name == "part_root");
        Assert.AreSame(attachedRoot, attachedPart.Parent);
        var attachedMesh = (MdlTrimeshNode)attachedPart.Children.Single();
        Assert.AreEqual("attachment-0", attachedMesh.Bitmap);
        Assert.IsFalse(attachedPart.ReceivesNamedAnimationPose);
    }

    [TestMethod]
    public void ComposeFlatKeepsIndependentPartTransformsAndAuthoredTextures()
    {
        var partRoot = new MdlNode { Name = "part", Position = new Vector3(2, 0, 0) };
        partRoot.Children.Add(new MdlTrimeshNode
        {
            Name = "mesh",
            Bitmap = "authored",
            Vertices = [Vector3.UnitX]
        });
        var part = new MdlModel { GeometryRoot = partRoot };

        var composed = new MdlPartComposer((resRef, _) => resRef == "part" ? part : null)
            .ComposeFlat(["part"], "composite");

        Assert.IsNotNull(composed);
        var attached = composed.GeometryRoot!.Children.Single();
        Assert.AreEqual(new Vector3(2, 0, 0), attached.Position);
        Assert.AreEqual("authored", ((MdlTrimeshNode)attached.Children.Single()).Bitmap);
    }
}
