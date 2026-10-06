using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Nwn.Toolset.Avalonia.Areas;
using Nwn.Toolset.Avalonia.Tests.Support;

namespace Nwn.Toolset.Avalonia.Tests.Areas;

[TestClass]
public sealed class AreaEditorLayoutTests
{
    [TestMethod]
    public async Task SwitchingPropertiesPreservesTheSceneAndEditorControlInstances()
    {
        Border scene = null!;
        TextBox properties = null!;
        AreaEditorLayout view = null!;
        await GraphTestRuntime.RunAsync(() =>
        {
            scene = new Border { MinHeight = 80 };
            properties = new TextBox { Text = "Retained edit" };
            view = new AreaEditorLayout { Scene = scene, Properties = properties };
            return view;
        }, window =>
        {
            window.Width = 330;
            window.Height = 500;
            Render(window);
            Assert.AreEqual(0, view.SelectedIndex);
            Assert.IsTrue(scene.Bounds.Height >= 400, $"Properties must not dock below and resize the map; scene {scene.Bounds}, view {view.Bounds}, window {window.Bounds}.");
            view.SelectedIndex = 1;
            Render(window);
            Assert.IsTrue(view.GetVisualDescendants().Contains(properties));
            properties.Text = "Unsaved edit";
            view.SelectedIndex = 0;
            Render(window);
            Assert.AreSame(scene, view.Scene);
            Assert.IsTrue(scene.Bounds.Height >= 400);
            view.SelectedIndex = 1;
            Render(window);
            Assert.AreSame(properties, view.Properties);
            Assert.AreEqual("Unsaved edit", properties.Text);
        });
    }
    private static void Render(Window window)
    {
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        window.UpdateLayout();
    }
}
