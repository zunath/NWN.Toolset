using Avalonia.Controls;
using Avalonia.VisualTree;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Resources;
using Nwn.Toolset.Avalonia.Explorer;
using Nwn.Toolset.Avalonia.Explorer.Views;
using Nwn.Toolset.Avalonia.Tests.Support;

namespace Nwn.Toolset.Avalonia.Tests.Explorer;

[TestClass]
public sealed class ModuleExplorerViewTests
{
    [TestMethod]
    public Task TheViewShowsTabsRowsAndLocalizedMenus()
    {
        var fixture = new ModuleExplorerFixture();
        fixture.Content.Add(ModuleResourceType.Nss, "one");
        fixture.SeededSection(ModuleResourceType.Nss).AddFolder("Folder");
        var controller = fixture.Open(ModuleResourceType.Nss);

        return GraphTestRuntime.RunAsync(
            () => new ModuleExplorerView { DataContext = controller },
            window =>
            {
                try
                {
                    var view = window.GetVisualDescendants().OfType<ModuleExplorerView>().Single();
                    var tree = view.FindControl<ListBox>("ModuleTree");
                    Assert.IsNotNull(tree);
                    Assert.AreEqual(2, tree.ItemCount);

                    var tabLabels = view.GetVisualDescendants().OfType<TextBlock>().Select(text => text.Text).ToList();
                    CollectionAssert.Contains(tabLabels, "Scripts");
                    CollectionAssert.Contains(tabLabels, "Folder");
                    CollectionAssert.Contains(tabLabels, "Unsorted");

                    Assert.IsTrue(view.GetVisualDescendants().OfType<TextBox>()
                        .Any(box => box.Watermark == "Search contents..."));

                    var newButton = view.GetVisualDescendants().OfType<Button>()
                        .Single(button => Equals(button.Content, "New Script..."));
                    Assert.IsTrue(newButton.IsVisible);

                    var folderRow = view.GetVisualDescendants().OfType<Grid>()
                        .Single(grid => grid.DataContext is ExplorerNodeViewModel { Name: "Folder" } && grid.ContextMenu != null);
                    Assert.IsNotNull(folderRow.ContextMenu);
                }
                finally
                {
                    fixture.Dispose();
                }
            });
    }
}
