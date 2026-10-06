using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Nwn.Preview.Areas;
using Nwn.Toolset.Avalonia.Areas;
using Nwn.Toolset.Avalonia.Localization;
using Nwn.Toolset.Avalonia.Tests.Support;

namespace Nwn.Toolset.Avalonia.Tests.Areas;

[TestClass]
public sealed class AreaCameraControlsTests
{
    [TestMethod]
    public async Task ExistingPadChangesOnlyItsOwnedViewportAndResetRestoresTheAreaFraming()
    {
        await GraphTestRuntime.RunAsync(() => new AreaCameraControls
        {
            Viewport = new AreaViewportControl
            {
                Scene = new AreaScene
                {
                    Tileset = "fixture_tile", Width = 2, Height = 2,
                    Tiles = [], Instances = [], Diagnostics = new(),
                },
            },
        }, window =>
        {
            var pad = (AreaCameraControls)window.Content!;
            var viewport = pad.Viewport!;
            var scene = viewport.Scene;
            var initial = viewport.CaptureViewportState()!.Value;
            Click(pad, AreaEditorStringId.OrbitLeft);
            Assert.IsTrue(viewport.CaptureViewportState()!.Value.Azimuth < initial.Azimuth);
            Click(pad, AreaEditorStringId.OrbitUp);
            Assert.IsTrue(viewport.CaptureViewportState()!.Value.Elevation > initial.Elevation);
            Click(pad, AreaEditorStringId.ZoomIn);
            Assert.IsTrue(viewport.CaptureViewportState()!.Value.Distance < initial.Distance);
            Click(pad, AreaEditorStringId.Reorient);
            Assert.AreEqual(initial, viewport.CaptureViewportState()!.Value);
            Assert.AreSame(scene, viewport.Scene);

            window.Width = 330;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            window.UpdateLayout();
            var buttons = pad.GetVisualDescendants().OfType<Button>().ToArray();
            Assert.AreEqual(11, buttons.Length);
            foreach (var button in buttons)
            {
                var origin = button.TranslatePoint(default, window);
                Assert.IsNotNull(origin);
                Assert.IsTrue(origin.Value.X >= 0 && origin.Value.X + button.Bounds.Width <= window.ClientSize.Width,
                    "Every camera action must remain reachable in a narrow editor.");
            }
            foreach (var repeat in buttons.OfType<RepeatButton>())
            {
                Assert.AreEqual(16, repeat.Delay);
                Assert.AreEqual(16, repeat.Interval);
            }
        });
    }

    private static void Click(AreaCameraControls pad, AreaEditorStringId id) =>
        pad.GetVisualDescendants().OfType<Button>()
            .Single(button => ToolTip.GetTip(button)?.ToString() == AreaEditorTexts.English.Get(id))
            .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
}
