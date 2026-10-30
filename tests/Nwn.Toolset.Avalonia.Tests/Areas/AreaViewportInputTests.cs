// SPDX-License-Identifier: MIT

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Nwn.Preview.Areas;
using Nwn.Toolset.Avalonia.Areas;
using Nwn.Toolset.Avalonia.Tests.Support;

namespace Nwn.Toolset.Avalonia.Tests.Areas;

[TestClass]
public sealed class AreaViewportInputTests
{
    private static readonly Point Start = new(300, 300);

    [TestMethod]
    public async Task MiddleDragTurnsTheSceneWithThePointer()
    {
        await GraphTestRuntime.RunAsync(CreateSurface, window =>
        {
            var viewport = ((AreaEditorSurface)window.Content!).Viewport;
            var initial = viewport.CaptureViewportState()!.Value;

            Drag(window, MouseButton.Middle, new Vector(1, 0));
            var afterRight = viewport.CaptureViewportState()!.Value;
            Assert.IsTrue(afterRight.Azimuth < initial.Azimuth, "A rightward drag carries the camera left around the target.");
            Assert.AreEqual(initial.Elevation, afterRight.Elevation);

            Drag(window, MouseButton.Middle, new Vector(0, 1));
            var afterDown = viewport.CaptureViewportState()!.Value;
            Assert.IsTrue(afterDown.Elevation > afterRight.Elevation, "A downward drag lifts the camera.");
        });
    }

    [TestMethod]
    public async Task CtrlLeftDragMovesTheCameraInsteadOfPicking()
    {
        await GraphTestRuntime.RunAsync(CreateSurface, window =>
        {
            var viewport = ((AreaEditorSurface)window.Content!).Viewport;
            var picks = 0;
            viewport.InstancePicked += _ => picks++;

            window.MouseDown(Start, MouseButton.Left, RawInputModifiers.Control);
            window.MouseUp(Start, MouseButton.Left, RawInputModifiers.Control);
            Assert.AreEqual(0, picks, "Ctrl+left is a camera drag, never a pick.");

            window.MouseDown(Start, MouseButton.Left);
            window.MouseUp(Start, MouseButton.Left);
            Assert.AreEqual(1, picks, "A plain left click still picks.");
        });
    }

    [TestMethod]
    public async Task WasdIsTakenByTheFocusedViewportButShortcutsPassThrough()
    {
        await GraphTestRuntime.RunAsync(CreateSurface, window =>
        {
            var viewport = ((AreaEditorSurface)window.Content!).Viewport;
            var handled = new List<(Key Key, bool Handled)>();
            window.AddHandler(InputElement.KeyDownEvent, (_, e) => handled.Add((e.Key, e.Handled)),
                RoutingStrategies.Bubble, handledEventsToo: true);

            window.MouseDown(Start, MouseButton.Left);
            window.MouseUp(Start, MouseButton.Left);
            Assert.IsTrue(viewport.IsFocused);

            foreach (var key in new[] { Key.W, Key.A, Key.S, Key.D })
            {
                window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null);
                window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null);
            }

            window.KeyPress(Key.S, RawInputModifiers.Control, PhysicalKey.None, null);
            window.KeyRelease(Key.S, RawInputModifiers.Control, PhysicalKey.None, null);
            Dispatcher.UIThread.RunJobs();

            CollectionAssert.AreEqual(
                new[] { (Key.W, true), (Key.A, true), (Key.S, true), (Key.D, true), (Key.S, false) },
                handled);
        });
    }

    private static AreaEditorSurface CreateSurface() => new()
    {
        Viewport =
        {
            Scene = new AreaScene
            {
                Tileset = "fixture_tile", Width = 2, Height = 2,
                Tiles = [], Instances = [], Diagnostics = new(),
            },
        },
    };

    /// <summary>
    /// Several small moves with real time between them: the orbit rate is per second, so moves that
    /// arrive with no elapsed time turn nothing.
    /// </summary>
    private static void Drag(Window window, MouseButton button, Vector step)
    {
        window.MouseDown(Start, button);
        var position = Start;
        for (var i = 0; i < 5; i++)
        {
            Thread.Sleep(10);
            position += step * 10;
            window.MouseMove(position);
        }

        window.MouseUp(position, button);
    }
}
