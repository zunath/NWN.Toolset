using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Resources;
using Nwn.Toolset.Avalonia.Palettes;
using Nwn.Toolset.Avalonia.Palettes.Views;
using Nwn.Toolset.Avalonia.Tests.Support;

namespace Nwn.Toolset.Avalonia.Tests.Palettes;

[TestClass]
public sealed class PaletteViewTests
{
    [TestMethod]
    public void MountedTileSearchKeepsSelectedCategoryAndDoesNotClearHostSelection()
    {
        var actions = new PaletteActionsSpy();
        using var state = new PalettePresentationState(actions);
        var hillCategoryId = new PaletteCategoryId("tiles/hills");
        var otherCategoryId = new PaletteCategoryId("tiles/other");
        var hillEntryId = new PaletteEntryId("tile/anthill-hill");
        var otherEntryId = new PaletteEntryId("tile/anthill-other");
        var hillCategory = new PaletteCategorySnapshot(hillCategoryId, "Hills", 1, false, 0,
            Array.Empty<PaletteCategorySnapshot>(), new[] { hillEntryId },
            new PaletteCategoryCapabilities(false, false, false, false, false, false, null));
        var otherCategory = new PaletteCategorySnapshot(otherCategoryId, "Other", 1, false, 1,
            Array.Empty<PaletteCategorySnapshot>(), new[] { otherEntryId },
            new PaletteCategoryCapabilities(false, false, false, false, false, false, null));
        var entries = new[]
        {
            new PaletteEntrySnapshot(hillEntryId, PaletteEntryKind.Tile, null, PaletteSource.Standard,
                "hill_ant", "AntHill", "", new[] { hillCategoryId }, 1, 1, Array.Empty<string>(),
                new PaletteEntryCapabilities(true, false, false, false, null)),
            new PaletteEntrySnapshot(otherEntryId, PaletteEntryKind.Tile, null, PaletteSource.Standard,
                "other_ant", "AntHill Other", "", new[] { otherCategoryId }, 1, 1, Array.Empty<string>(),
                new PaletteEntryCapabilities(true, false, false, false, null))
        };
        state.SetSnapshot(new PaletteSnapshot(1, PaletteMode.Tiles, null, PaletteSource.Custom,
            new[] { new PaletteTypeOption(null, "Tiles", "T", "New", null) },
            new[] { hillCategory, otherCategory }, entries, PaletteTilePaintMode.Auto, true, null,
            new PaletteCapabilities(false, false, false, false)));

        var selectedRowChanges = new List<PaletteCategoryId?>();
        state.PropertyChanged += (_, change) =>
        {
            if (change.PropertyName == nameof(PalettePresentationState.SelectedRow))
            {
                selectedRowChanges.Add(state.SelectedRow?.Id);
            }
        };
        GraphTestRuntime.RunAsync(() => new PaletteView { DataContext = state }, window =>
        {
            var view = (PaletteView)window.Content!;
            view.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var categoryList = view.GetVisualDescendants().OfType<ListBox>()
                .Single(list => ReferenceEquals(list.ItemsSource, state.Rows));
            var searchField = view.GetVisualDescendants().OfType<TextBox>().Single();
            var categoryPoint = categoryList.TranslatePoint(new Point(24, 12), window)
                ?? throw new AssertFailedException("The selected tile category is mounted in the visible list.");
            window.MouseDown(categoryPoint, MouseButton.Left);
            window.MouseUp(categoryPoint, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();

            Assert.AreEqual(hillCategoryId, state.SelectedRow?.Id,
                "Selecting the mounted category list routes the user's category selection into the palette state.");
            CollectionAssert.AreEqual(new[] { hillCategoryId }, actions.SelectedCategories,
                "The selected category is sent to the host before searching.");

            var changesBeforeSearch = selectedRowChanges.Count;
            searchField.Focus();
            searchField.Text = " ";
            categoryList.Focus();
            Dispatcher.UIThread.RunJobs();
            Assert.AreEqual(" ", state.Query, "The mounted field accepts a whitespace-only search query.");
            Assert.AreEqual(hillCategoryId, state.SelectedRow?.Id,
                "A query that normalizes to no search still preserves the visible category selection.");
            CollectionAssert.AreEqual(new[] { hillCategoryId }, actions.SelectedCategories,
                "Rebuilding visible rows does not send a transient null category to the host.");
            Assert.IsFalse(selectedRowChanges.Skip(changesBeforeSearch).Any(id => id is null),
                "A query-only projection does not clear the selected category in the mounted control.");

            searchField.Focus();
            searchField.Text = "AntHill";
            categoryList.Focus();
            Dispatcher.UIThread.RunJobs();
            Assert.AreEqual("AntHill", state.Query, "Typing in the mounted search field updates the bound tile query.");
            Dispatcher.UIThread.RunJobs();
            view.UpdateLayout();

            Assert.AreEqual(hillCategoryId, state.SelectedRow?.Id,
                "Rebuilding visible category rows for a tile query retains the selected category.");
            Assert.AreEqual(hillCategoryId, (categoryList.SelectedItem as PaletteCategoryRow)?.Id,
                "The mounted ListBox keeps the selected row after its ItemsSource is rebuilt.");
            Assert.AreEqual(hillEntryId, state.Tiles.Single().Id,
                "The query returns the matching tile from the selected category only.");
            CollectionAssert.AreEqual(new[] { hillCategoryId }, actions.SelectedCategories,
                "Projection rebuilds must not send a transient null selection to the host.");

            searchField.Focus();
            searchField.Text = string.Empty;
            categoryList.Focus();
            Dispatcher.UIThread.RunJobs();
            view.UpdateLayout();

            Assert.AreEqual(string.Empty, state.Query, "Clearing the mounted search field returns to the selected category.");
            Assert.AreEqual(hillCategoryId, state.SelectedRow?.Id,
                "Returning from query results retains the previously selected tile category.");
            Assert.AreEqual(hillEntryId, state.Tiles.Single().Id,
                "Clearing the query restores the matching tile from the retained category.");
            CollectionAssert.AreEqual(new[] { hillCategoryId }, actions.SelectedCategories,
                "Clearing the query does not route a transient null category selection to the host.");
        });
    }
    [TestMethod]
    public async Task MountedViewExchangesSwitchesWithoutMovingSearchAndRendersCappedPaletteSectionsAsync()
    {
        var actions = new PaletteActionsSpy();
        using var state = new PalettePresentationState(actions);
        var categoryId = new PaletteCategoryId("utp/custom/root");
        var entryIds = Enumerable.Range(0, 220)
            .Select(index => new PaletteEntryId($"utp/custom/item{index:D3}"))
            .ToArray();
        var entries = entryIds.Select((id, index) => new PaletteEntrySnapshot(
            id, PaletteEntryKind.Blueprint, ModuleResourceType.Utp, PaletteSource.Custom,
            $"item{index:D3}", $"Entry {index:D3}", $"item{index:D3}", new[] { categoryId },
            null, null, Array.Empty<string>(), new PaletteEntryCapabilities(true, true, true, true, null)))
            .ToArray();
        var category = new PaletteCategorySnapshot(categoryId, "Buildings", entries.Length, false, 0,
            Array.Empty<PaletteCategorySnapshot>(), entryIds, new PaletteCategoryCapabilities(true, true, true, true, true, true, null));
        state.SetSnapshot(new PaletteSnapshot(1, PaletteMode.Blueprints, ModuleResourceType.Utp,
            PaletteSource.Custom,
            new[] { new PaletteTypeOption(ModuleResourceType.Utp, "Placeables", "P", "New Placeable...", null),
                new PaletteTypeOption(null, "Tiles", "T", "New", null) },
            new[] { category }, entries, PaletteTilePaintMode.Auto, true, null,
            new PaletteCapabilities(true, true, true, true)));
        state.SelectedRow = state.Rows[0];
        state.Query = "Entry";
        Assert.AreEqual(PalettePresentationState.MaxSearchResults, state.Tiles.Count);

        await GraphTestRuntime.RunAsync(() => new PaletteView { DataContext = state }, window =>
        {
            var view = (PaletteView)window.Content!;
            view.UpdateLayout();
            var lists = view.GetVisualDescendants().OfType<ListBox>().ToArray();
            Assert.IsTrue(view.GetVisualDescendants().OfType<TextBox>().Any(), "The search field is mounted.");
            var settingsButton = view.GetVisualDescendants().OfType<Button>()
                .Single(button => ToolTip.GetTip(button)?.ToString() == state.Texts.GridSettings);
            Assert.IsInstanceOfType<Flyout>(settingsButton.Flyout);
            var settingsFlyout = (Flyout)settingsButton.Flyout!;
            settingsFlyout.ShowAt(settingsButton);
            Dispatcher.UIThread.RunJobs();
            Assert.IsTrue((settingsFlyout.Content as Control)?.GetVisualDescendants().OfType<Slider>().Any(),
                "The settings flyout opens with the tile-size control.");
            settingsFlyout.Hide();
            Assert.IsTrue(view.GetVisualDescendants().OfType<ItemsControl>().Any(control => ReferenceEquals(control.ItemsSource, state.Types)),
                "The type row binds to the shared palette state.");
            Assert.IsTrue(lists.Any(control => ReferenceEquals(control.ItemsSource, state.Rows)),
                "The category tree binds to the shared palette state.");
            Assert.IsTrue(lists.Any(control => ReferenceEquals(control.ItemsSource, state.Tiles)),
                "The virtualized tile grid binds to the shared palette state.");

            var switches = view.GetVisualDescendants().OfType<StackPanel>().ToArray();
            var sourceSwitch = switches.Single(panel => panel.Children.OfType<Button>()
                .Any(button => button.Content?.ToString() == state.Texts.CustomSource));
            var tilePaintSwitch = switches.Single(panel => panel.Children.OfType<Button>()
                .Any(button => button.Content?.ToString() == state.Texts.AutoTilePaint));
            var searchField = view.GetVisualDescendants().OfType<TextBox>().Single();
            var searchY = searchField.Bounds.Y;
            Assert.IsTrue(sourceSwitch.IsVisible);
            Assert.IsTrue(sourceSwitch.IsEnabled);
            Assert.IsFalse(tilePaintSwitch.IsVisible);

            state.SetSnapshot(new PaletteSnapshot(2, PaletteMode.Tiles, null, PaletteSource.Custom,
                new[] { new PaletteTypeOption(ModuleResourceType.Utp, "Placeables", "P", "New Placeable...", null),
                    new PaletteTypeOption(null, "Tiles", "T", "New", null) },
                Array.Empty<PaletteCategorySnapshot>(), Array.Empty<PaletteEntrySnapshot>(),
                PaletteTilePaintMode.Auto, false, null, new PaletteCapabilities(true, true, true, true)));
            Dispatcher.UIThread.RunJobs();
            view.UpdateLayout();
            Assert.IsFalse(sourceSwitch.IsVisible, "Tiles mode hides blueprint source selection.");
            Assert.IsTrue(tilePaintSwitch.IsVisible, "Tiles mode shows tile-paint selection.");
            Assert.AreEqual(searchY, searchField.Bounds.Y,
                "The mode-specific switch replaces its counterpart in the same row.");
        });
    }
}
