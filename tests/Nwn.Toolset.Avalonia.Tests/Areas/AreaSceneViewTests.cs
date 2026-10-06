using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Nwn.Preview.Areas;
using Nwn.Toolset.Avalonia.Areas;
using Nwn.Toolset.Avalonia.Localization;
using Nwn.Toolset.Avalonia.Tests.Support;

namespace Nwn.Toolset.Avalonia.Tests.Areas;

[TestClass]
public sealed class AreaSceneViewTests
{
    [TestMethod]
    public async Task HeldRotationPreviewsAndCaptureLossCommitsOneEditWithoutResizingTheMapAsync()
    {
        var marker = new InstanceMarker { Kind = InstanceMarkerKind.Placeable, ListIndex = 0,
            Position = new Vector3(3, 4, 0), Orientation = Vector2.UnitX, Tag = "crate" };
        await GraphTestRuntime.RunAsync(() =>
        {
            var view = new AreaSceneView { Overlay = new() { HasSceneSelection = true, CanRotateSelection = true } };
            view.Viewport.Scene = new AreaScene { Tileset = "fixture", Width = 2, Height = 2,
                Tiles = [], Instances = [marker], Diagnostics = new() };
            view.Viewport.SelectedInstance = marker;
            return view;
        }, window =>
        {
            var view = (AreaSceneView)window.Content!;
            var scene = view.Viewport.Scene;
            var bounds = view.Surface.Bounds;
            var commits = new List<Vector2>();
            view.Viewport.InstanceRotated += (instance, orientation) =>
            {
                Assert.AreSame(marker, instance);
                commits.Add(orientation);
            };
            var rotate = Button(view, AreaEditorStringId.RotateClockwise);
            for (var index = 0; index < 4; index++) rotate.RaiseEvent(new RoutedEventArgs(global::Avalonia.Controls.Button.ClickEvent));
            Assert.IsTrue(view.IsDragging);
            StringAssert.StartsWith(view.DragFacing, "facing ");
            Assert.AreEqual(0, commits.Count, "A held preview cannot commit a history entry on each tick.");
            Render(window);
            Assert.AreEqual(bounds, view.Surface.Bounds, "The drag notice floats over the map without resizing it.");
            rotate.RaiseEvent(new PointerCaptureLostEventArgs(rotate, new Pointer(1, PointerType.Mouse, true)));
            Assert.AreEqual(1, commits.Count);
            Assert.AreNotEqual(Vector2.UnitX, commits[0]);
            Assert.IsFalse(view.IsDragging);
            Assert.AreSame(scene, view.Viewport.Scene);
            Assert.AreEqual(Vector2.UnitX, marker.Orientation, "The host owns the document commit.");
            rotate.RaiseEvent(new PointerCaptureLostEventArgs(rotate, new Pointer(2, PointerType.Mouse, true)));
            Assert.AreEqual(1, commits.Count);
            Button(view, AreaEditorStringId.RotateRandomly).RaiseEvent(new RoutedEventArgs(global::Avalonia.Controls.Button.ClickEvent));
            Assert.AreEqual(2, commits.Count);
            Assert.AreEqual(1f, commits[1].Length(), 0.0001f);
        });
    }

    [TestMethod]
    public async Task TileActionsRespectSelectionAndEveryPadActionRemainsReachableInANarrowSceneAsync()
    {
        await GraphTestRuntime.RunAsync(() => new AreaSceneView(), window =>
        {
            var view = (AreaSceneView)window.Content!;
            var raises = 0;
            var lowers = 0;
            view.RaiseTileRequested += (sender, args) => raises++;
            view.LowerTileRequested += (sender, args) => lowers++;
            view.RaiseTileButton.RaiseEvent(new RoutedEventArgs(global::Avalonia.Controls.Button.ClickEvent));
            view.LowerTileButton.RaiseEvent(new RoutedEventArgs(global::Avalonia.Controls.Button.ClickEvent));
            Assert.AreEqual(0, raises);
            Assert.AreEqual(0, lowers);
            Assert.IsFalse(view.Overlay.HasViewportHud);
            view.Overlay = new() { HasTileSelection = true, TileSelectionStatus = "Tile 0, 0" };
            view.RaiseTileButton.RaiseEvent(new RoutedEventArgs(global::Avalonia.Controls.Button.ClickEvent));
            view.LowerTileButton.RaiseEvent(new RoutedEventArgs(global::Avalonia.Controls.Button.ClickEvent));
            Assert.AreEqual(1, raises);
            Assert.AreEqual(1, lowers);
            Assert.IsTrue(view.Overlay.HasViewportHud);
            view.Overlay = view.Overlay with { IsBuildingScene = true };
            Assert.IsFalse(view.Overlay.HasViewportHud, "The centered build notice replaces the corner notice.");
            window.Width = 330;
            Render(window);
            var buttons = view.GetVisualDescendants().OfType<Button>().ToArray();
            Assert.AreEqual(16, buttons.Length);
            foreach (var button in buttons)
            {
                var origin = button.TranslatePoint(default, window);
                Assert.IsNotNull(origin);
                Assert.IsTrue(origin.Value.X >= 0 && origin.Value.X + button.Bounds.Width <= window.ClientSize.Width,
                    $"Pad action {ToolTip.GetTip(button)} lies outside the narrow scene: {origin}, {button.Bounds}.");
            }
        });
    }

    private static Button Button(AreaSceneView view, AreaEditorStringId id) =>
        view.GetVisualDescendants().OfType<Button>().Single(button =>
            ToolTip.GetTip(button)?.ToString() == AreaEditorTexts.English.Get(id));

    private static void Render(Window window)
    {
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        window.UpdateLayout();
    }
}
