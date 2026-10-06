using System.Numerics;
using Nwn.Toolset.Avalonia.Viewport;
using Nwn.Preview.Scene;

namespace Nwn.Toolset.Avalonia.Tests.Viewport;

[TestClass]
public sealed class OrbitCameraStateTests
{
    [TestMethod]
    public void FitOrbitPanAndZoom_KeepCameraBoundedAndMoveExpectedAxes()
    {
        var camera = new OrbitCameraState();
        camera.Fit(new Vector3(2, 3, 4), 0.5f);
        var start = camera.Position;
        camera.Orbit(50, 15);
        camera.Pan(12, -8);
        camera.Zoom(1.5f);

        Assert.AreNotEqual(new Vector3(2, 3, 4), camera.Target);
        Assert.AreNotEqual(start, camera.Position);
        Assert.IsTrue(camera.Distance > 0.05f && camera.Distance < 100_000f);
        Assert.IsTrue(float.IsFinite(camera.Position.X) && float.IsFinite(camera.Position.Y) && float.IsFinite(camera.Position.Z));
    }

    [TestMethod]
    public void FitFromPreparedSceneBoundsMatchesVertexFitCameraValues()
    {
        var minimum = new Vector3(-1, 2, 0);
        var maximum = new Vector3(1, 4, 2);
        var bounds = new PreparedSceneBounds(minimum, maximum);
        var legacyCenter = (minimum + maximum) * 0.5f;
        var legacyRadius = Math.Max(0.01f, Vector3.Distance(minimum, maximum) * 0.5f);
        var camera = new OrbitCameraState();

        camera.Fit(bounds.Center, bounds.Radius);

        Assert.AreEqual(legacyCenter, camera.Target);
        Assert.AreEqual(Math.Clamp(legacyRadius * 2.8f, 0.05f, 100_000f), camera.Distance);
    }
    [TestMethod]
    public void FitRejectsInvalidSceneBoundsAndZoomClampsExtremeValues()
    {
        var camera = new OrbitCameraState();
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => camera.Fit(new Vector3(float.NaN), 1));
        camera.Zoom(float.MaxValue);
        Assert.IsTrue(float.IsFinite(camera.Distance));
        Assert.IsTrue(camera.Distance >= 0.05f);
        camera.Zoom(-float.MaxValue);
        Assert.IsTrue(camera.Distance <= 100_000f);
    }
}
