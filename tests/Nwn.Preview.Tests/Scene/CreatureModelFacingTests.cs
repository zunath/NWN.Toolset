using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Preview.Scene;

namespace Nwn.Preview.Tests.Scene;

[TestClass]
public sealed class CreatureModelFacingTests
{
    [TestMethod]
    public void ForwardCorrectionMapsCreatureModelForwardOntoAreaHeadingAxis()
    {
        var corrected = Vector3.Transform(Vector3.UnitY, CreatureModelFacing.ForwardCorrection);

        Assert.AreEqual(1f, corrected.X, 0.0001f);
        Assert.AreEqual(0f, corrected.Y, 0.0001f);
        Assert.AreEqual(0f, corrected.Z, 0.0001f);
    }
}
