using Vector = Avalonia.Vector;
using System.Numerics;
using System.Reflection;
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

    [TestMethod]
    public async Task CtrlBothButtonsMovesOnlyCameraHeightAndReleasingRightResumesPan()
    {
        await GraphTestRuntime.RunAsync(CreateSurface, window =>
        {
            var viewport = ((AreaEditorSurface)window.Content!).Viewport;
            SetFrame(viewport);
            var before = viewport.CaptureViewportState()!.Value;
            var picks = 0;
            viewport.InstancePicked += _ => picks++;
            window.MouseDown(Start, MouseButton.Left, RawInputModifiers.Control);
            window.MouseDown(Start, MouseButton.Right, RawInputModifiers.Control | RawInputModifiers.LeftMouseButton);
            window.MouseMove(Start + new Vector(20, 30), RawInputModifiers.Control | RawInputModifiers.LeftMouseButton | RawInputModifiers.RightMouseButton);
            var elevated = viewport.CaptureViewportState()!.Value;
            Assert.AreEqual(before.Target.X, elevated.Target.X);
            Assert.AreEqual(before.Target.Y, elevated.Target.Y);
            Assert.AreNotEqual(before.Target.Z, elevated.Target.Z);
            window.MouseUp(Start + new Vector(20, 30), MouseButton.Right, RawInputModifiers.Control | RawInputModifiers.LeftMouseButton);
            window.MouseMove(Start + new Vector(50, 30), RawInputModifiers.Control | RawInputModifiers.LeftMouseButton);
            var panned = viewport.CaptureViewportState()!.Value;
            Assert.AreEqual(elevated.Target.Z, panned.Target.Z);
            Assert.AreNotEqual(elevated.Target, panned.Target);
            window.MouseUp(Start + new Vector(50, 30), MouseButton.Left, RawInputModifiers.Control);
            Assert.AreEqual(0, picks);
        });
    }

    [TestMethod]
    public async Task ShiftLeftPlacesRepeatedlyAndPlainLeftEndsTheBrush()
    {
        await GraphTestRuntime.RunAsync(CreateSurface, window =>
        {
            var viewport = ((AreaEditorSurface)window.Content!).Viewport;
            SetFrame(viewport);
            viewport.IsPlacementActive = true;
            var picks = new List<PlacementPick>();
            viewport.PlacementPointPicked += picks.Add;
            var point = new Point(500, 325);
            for (var i = 0; i < 2; i++)
            {
                window.MouseDown(point, MouseButton.Left, RawInputModifiers.Shift);
                window.MouseUp(point, MouseButton.Left, RawInputModifiers.Shift);
            }
            Assert.HasCount(2, picks);
            Assert.IsTrue(picks.All(pick => pick.KeepBrush));
            Assert.IsTrue(viewport.IsPlacementActive);
            window.MouseDown(point, MouseButton.Left);
            window.MouseUp(point, MouseButton.Left);
            Assert.HasCount(3, picks);
            Assert.IsFalse(picks[2].KeepBrush);
            Assert.IsFalse(viewport.IsPlacementActive);
        });
    }

    [TestMethod]
    public async Task ShiftRightRotatesThePickedObjectInsteadOfOpeningAContextMenu()
    {
        await GraphTestRuntime.RunAsync(CreateSurface, window =>
        {
            var viewport = ((AreaEditorSurface)window.Content!).Viewport;
            SetFrame(viewport);
            var marker = new InstanceMarker { Kind = InstanceMarkerKind.Placeable, ListIndex = 0,
                Position = new Vector3(10, 10, 0), Orientation = Vector2.UnitX, Tag = "crate" };
            viewport.Scene = viewport.Scene!.WithInstanceAdded(marker);
            viewport.InstancePicked += hit => viewport.SelectedInstance = hit;
            Vector2? rotated = null;
            viewport.InstanceRotated += (_, orientation) => rotated = orientation;
            var point = new Point(500, 325);
            window.MouseDown(point, MouseButton.Right, RawInputModifiers.Shift);
            window.MouseMove(point + new Vector(50, 0), RawInputModifiers.Shift);
            window.MouseUp(point + new Vector(50, 0), MouseButton.Right, RawInputModifiers.Shift);
            Assert.IsNotNull(rotated);
            Assert.AreNotEqual(Vector2.UnitX, rotated.Value);
            Assert.IsTrue(viewport.SuppressContextMenu);
        });
    }

    [TestMethod]
    public async Task ShiftRightOnGroundRequestsTheTileVariantWithoutACameraDrag()
    {
        await GraphTestRuntime.RunAsync(CreateSurface, window =>
        {
            var viewport = ((AreaEditorSurface)window.Content!).Viewport;
            SetFrame(viewport);
            viewport.TileSelected += cell => viewport.SelectedTileCell = cell;
            var cycles = 0;
            viewport.TileVariantRequested += (_, _) => cycles++;
            var before = viewport.CaptureViewportState();
            var point = new Point(500, 325);
            window.MouseDown(point, MouseButton.Right, RawInputModifiers.Shift);
            window.MouseUp(point, MouseButton.Right, RawInputModifiers.Shift);
            Assert.AreEqual(1, cycles);
            Assert.AreEqual(before, viewport.CaptureViewportState());
        });
    }

    [TestMethod]
    public async Task RectangleSelectionSelectsMultipleObjectsAndShiftClickExtends()
    {
        await GraphTestRuntime.RunAsync(CreateSurface, window =>
        {
            var viewport = ((AreaEditorSurface)window.Content!).Viewport;
            SetFrame(viewport);
            var first = new InstanceMarker { Kind = InstanceMarkerKind.Placeable, ListIndex = 0,
                Position = new Vector3(8, 10, 0), Orientation = Vector2.UnitX };
            var second = new InstanceMarker { Kind = InstanceMarkerKind.Creature, ListIndex = 0,
                Position = new Vector3(12, 10, 0), Orientation = Vector2.UnitX };
            viewport.Scene = viewport.Scene!.WithInstanceAdded(first).WithInstanceAdded(second);
            AreaSelectionPick? picked = null;
            viewport.SelectionPicked += selection => picked = selection;
            window.MouseDown(new Point(400, 250), MouseButton.Left);
            window.MouseMove(new Point(600, 400));
            window.MouseUp(new Point(600, 400), MouseButton.Left);
            Assert.IsNotNull(picked);
            Assert.HasCount(2, picked.Instances);
            Assert.IsFalse(picked.ExtendSelection);
            var point = new Point(500, 325);
            window.MouseDown(point, MouseButton.Left, RawInputModifiers.Shift);
            window.MouseUp(point, MouseButton.Left, RawInputModifiers.Shift);
            Assert.IsTrue(picked.ExtendSelection);
        });
    }

    [TestMethod]
    public async Task CtrlPanTakesPriorityOverTheSelectedObjectGizmo()
    {
        await GraphTestRuntime.RunAsync(CreateSurface, window =>
        {
            var viewport = ((AreaEditorSurface)window.Content!).Viewport;
            SetFrame(viewport);
            var marker = new InstanceMarker { Kind = InstanceMarkerKind.Placeable, ListIndex = 0,
                Position = new Vector3(10, 10, 0), Orientation = Vector2.UnitX };
            viewport.Scene = viewport.Scene!.WithInstanceAdded(marker);
            viewport.SelectedInstance = marker;
            var moved = 0;
            viewport.InstanceMoved += (_, _) => moved++;
            var before = viewport.CaptureViewportState()!.Value;
            var point = new Point(500, 325);
            window.MouseDown(point, MouseButton.Left, RawInputModifiers.Control);
            window.MouseMove(point + new Vector(40, 0), RawInputModifiers.Control | RawInputModifiers.LeftMouseButton);
            window.MouseUp(point + new Vector(40, 0), MouseButton.Left, RawInputModifiers.Control);
            Assert.AreEqual(0, moved);
            Assert.AreNotEqual(before.Target, viewport.CaptureViewportState()!.Value.Target);
        });
    }

    private static void SetFrame(AreaViewportControl viewport)
    {
        void Set(string name, object value) => typeof(AreaViewportControl)
            .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(viewport, value);
        Set("_viewportWidth", 1000); Set("_viewportHeight", 650);
        Set("_lastView", Matrix4x4.CreateLookAt(new Vector3(10, -20, 20), new Vector3(10, 10, 0), Vector3.UnitZ));
        Set("_lastProjection", Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4, 1000f / 650, 0.1f, 1000f));
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
