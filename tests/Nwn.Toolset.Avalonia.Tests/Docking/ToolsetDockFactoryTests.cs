using global::Avalonia.Controls;
using global::Avalonia.Controls.Templates;
using global::Avalonia.Threading;
using global::Avalonia.VisualTree;
using Dock.Avalonia.Controls;
using Dock.Model.Core;
using Dock.Model.Controls;
using Dock.Model.Mvvm.Controls;
using Nwn.Toolset.Avalonia.Docking;
using Nwn.Toolset.Avalonia.Tests.Support;
using AvaloniaDock = global::Avalonia.Controls.Dock;

namespace Nwn.Toolset.Avalonia.Tests.Docking;

[TestClass]
public sealed class ToolsetDockFactoryTests
{
    [TestMethod]
    public void HostPanelsRetainTheirIdentityAndSavedDividersInTheMovedLayout()
    {
        var explorer = new Tool { Id = "ModuleContents", Title = "Resources" };
        var contents = new Tool { Id = "AreaContents", Title = "Contents" };
        var palette = new Tool { Id = "Palette", Title = "Palette" };
        var output = new Tool { Id = "Output", Title = "Output" };
        var factory = new ToolsetDockFactory(new([explorer, contents], [palette], [output]),
            new Dictionary<string, double> { [nameof(ToolsetDockId.ExplorerDock)] = .31 });
        var root = factory.CreateLayout();
        factory.InitLayout(root);
        var left = (IToolDock)DockProportions.Walk(root).Single(dock => dock.Id == nameof(ToolsetDockId.ExplorerDock));
        var right = (IToolDock)DockProportions.Walk(root).Single(dock => dock.Id == nameof(ToolsetDockId.PaletteDock));
        Assert.AreSame(explorer, left.ActiveDockable);
        Assert.AreSame(contents, left.VisibleDockables![1]);
        Assert.AreEqual(.31, left.Proportion);
        Assert.AreEqual(Alignment.Left, left.Alignment);
        Assert.AreEqual(Alignment.Right, right.Alignment);
        Assert.AreEqual(GripMode.Hidden, left.GripMode);
        Assert.AreSame(palette, right.ActiveDockable);
        Assert.IsTrue(DockProportions.Walk(root).All(dock => dock.DockCapabilityOverrides is not null));
        Assert.AreEqual(.31, factory.CaptureProportions()[nameof(ToolsetDockId.ExplorerDock)]);
    }

    [TestMethod]
    public void DocumentActivationAndRefusedCloseUseTheHostGuard()
    {
        var factory = new ToolsetDockFactory(new([new Tool { Id = "Resources" }],
            [new Tool { Id = "Palette" }], [new Tool { Id = "Output" }]));
        var root = factory.CreateLayout();
        factory.InitLayout(root);
        Document? active = null;
        factory.ActiveDocumentChanged += document => active = document;
        var first = new GuardedDocument { Id = "Area" };
        var second = new Document { Id = "Blueprint" };
        factory.OpenDocument(first);
        factory.OpenDocument(second);
        Assert.AreSame(second, active);
        factory.ActivateDocument(first);
        Assert.AreSame(first, active);
        first.AllowClose = false;
        factory.CloseDocument(first);
        Assert.IsTrue(DockProportions.Walk(root).Contains(first));
        first.AllowClose = true;
        factory.CloseDocument(first);
        Assert.IsFalse(DockProportions.Walk(root).Contains(first));
        Assert.AreSame(second, factory.ActiveDocument);
    }

    [TestMethod]
    public Task MovedThemeAndRailTabsRenderWithNativeDockControls() => GraphTestRuntime.RunAsync(() =>
    {
        RailToolTabs.Register();
        var explorer = new Tool { Id = "ModuleContents", Title = "Module Contents" };
        var contents = new Tool { Id = "AreaContents", Title = "Area Contents" };
        var palette = new Tool { Id = "Palette", Title = "Palette" };
        var output = new Tool { Id = "Output", Title = "Output" };
        var factory = new ToolsetDockFactory(new([explorer, contents], [palette], [output]));
        var root = factory.CreateLayout();
        factory.InitLayout(root);
        factory.OpenDocument(new Document { Id = "Area", Title = "Area" });
        var dock = new DockControl { Layout = root };
        dock.DataTemplates.Add(new FuncDataTemplate<Tool>((tool, scope) => new TextBlock { Text = tool!.Title }));
        dock.DataTemplates.Add(new FuncDataTemplate<Document>((document, scope) => new TextBlock { Text = document!.Title }));
        return dock;
    }, window =>
    {
        var controls = window.GetVisualDescendants().OfType<ToolControl>().ToArray();
        Assert.AreEqual(3, controls.Length);
        foreach (var control in controls)
        {
            var model = (IDock)control.DataContext!;
            var strip = control.GetVisualDescendants().OfType<ToolTabStrip>().Single();
            var expected = model.Id == nameof(ToolsetDockId.ExplorerDock) ? AvaloniaDock.Top : AvaloniaDock.Bottom;
            Assert.AreEqual(expected, DockPanel.GetDock(strip));
            Assert.AreEqual(expected == AvaloniaDock.Top, strip.Classes.Contains(RailToolTabs.StripClass));
        }
        Assert.IsTrue(window.GetVisualDescendants().OfType<TextBlock>().Any(text => text.Text == "Module Contents"));
    });

    private sealed class GuardedDocument : Document
    {
        public bool AllowClose { get; set; }
        public override bool OnClose() => AllowClose;
    }
}
