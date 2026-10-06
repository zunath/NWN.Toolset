using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Areas.Tiles;
using Nwn.Authoring.Categories;
using Nwn.Authoring.Resources;
using Nwn.Toolset.Avalonia.Palettes;
using Nwn.Toolset.Avalonia.Palettes.Workflow;
using Nwn.Toolset.Avalonia.Tests.Support;

namespace Nwn.Toolset.Avalonia.Tests.Palettes.Workflow;

[TestClass]
public sealed class PaletteWorkflowSnapshotTests
{
    [TestMethod]
    public void CustomSnapshotProjectsFoldersPinsUnsortedNamesAndCapabilities()
    {
        using var palette = new PaletteWorkflowFixture();
        palette.Content.AddCustom(ModuleResourceType.Utp, "crate", "Crate");
        palette.Content.AddCustom(ModuleResourceType.Utp, "chair");
        palette.Content.AddCustom(ModuleResourceType.Utp, "loose");
        var section = palette.Categories.Live.Section(ModuleResourceType.Utp);
        var furniture = section.AddFolder("Furniture");
        furniture.AddMember("chair");
        var containers = furniture.AddChild("Containers");
        containers.AddMember("crate");
        containers.AddMember("ghost");
        section.Pin(section.PathKey(containers));
        palette.Placement.ActiveTarget = new FakePalettePlacementTarget();

        palette.Controller.Refresh();

        var roots = palette.State.Rows.Select(row => row.Category).Distinct().ToList();
        var furnitureRow = roots.Single(category => category.Name == "Furniture");
        Assert.AreEqual(2, furnitureRow.Count, "Counts include descendants and only existing members.");
        var pinned = roots.Single(category => category.Name == "Containers");
        Assert.IsTrue(pinned.IsPinned);
        Assert.AreEqual(1, pinned.EntryIds.Count, "A member with no blueprint behind it is not listed.");
        var unsorted = roots.Single(category => category.Name == "Unsorted");
        Assert.AreEqual(int.MaxValue, unsorted.PinOrder);
        Assert.AreEqual(1, unsorted.Count);
        Assert.IsFalse(unsorted.Capabilities.CanRename);
        Assert.IsTrue(unsorted.Capabilities.CanCreateCategory);

        palette.SelectRow("Unsorted");
        Assert.AreEqual("loose", palette.Tile("loose").Name, "An unnamed blueprint shows its resref.");

        palette.SelectRow("Containers");
        var crate = palette.Tile("crate").Snapshot;
        Assert.AreEqual("Crate", crate.Name);
        Assert.AreEqual("crate", crate.Subtitle);
        Assert.AreEqual(ModuleResourceType.Utp, crate.ResourceType);
        Assert.IsTrue(crate.Capabilities.CanPlace);
        Assert.IsTrue(crate.Capabilities.CanEdit);
        Assert.IsTrue(crate.Capabilities.CanEditCopy);
        Assert.IsTrue(crate.Capabilities.CanDelete);
        Assert.IsNull(crate.Capabilities.ReadOnlyNotice);
    }

    [TestMethod]
    public void StandardSourceShowsBaseGameNamesReadOnlyAndRemembersTheSwitch()
    {
        using var palette = new PaletteWorkflowFixture();
        palette.Content.StandardPalettes[ModuleResourceType.Utp] = StandardBoxes();

        palette.Controller.SelectSource(PaletteSource.Standard);

        Assert.AreEqual(PaletteSource.Standard, palette.Settings.Source);
        Assert.IsFalse(palette.Controller.CanWrite);
        Assert.IsTrue(palette.Controller.CanEditCopy);
        var containers = palette.SelectRow("Containers");
        Assert.IsFalse(containers.Capabilities.CanRename);
        Assert.AreEqual("Base game content - read-only", containers.Capabilities.ReadOnlyNotice);
        var box = palette.Tile("std_box").Snapshot;
        Assert.AreEqual("Box", box.Name);
        Assert.AreEqual(PaletteSource.Standard, box.Source);
        Assert.IsFalse(box.Capabilities.CanEdit);
        Assert.IsTrue(box.Capabilities.CanEditCopy);
        Assert.IsFalse(box.Capabilities.CanDelete);
        Assert.AreEqual("Base game content - read-only", box.Capabilities.ReadOnlyNotice);
    }

    [TestMethod]
    public void SavedPreferencesAreRestoredWithoutBeingWrittenBack()
    {
        using var palette = new PaletteWorkflowFixture();
        palette.Settings.Selection = PaletteSelection.ForType(ModuleResourceType.Utc);
        palette.Settings.Source = PaletteSource.Standard;
        palette.Settings.PreviewSize = 150;
        palette.Settings.TilePaintMode = TilePaintMode.Manual;
        palette.Settings.CategoryProportion = 0.4;

        var controller = palette.Controller;

        Assert.AreEqual(ModuleResourceType.Utc, controller.SelectedType);
        Assert.AreEqual(PaletteSource.Standard, controller.Source);
        Assert.AreEqual(150, controller.TileSize);
        Assert.AreEqual(TilePaintMode.Manual, controller.TilePaintMode);
        Assert.AreEqual(0.4, controller.CategoryProportion);
        Assert.AreEqual(PaletteSelection.ForType(ModuleResourceType.Utc), palette.Settings.Selection);

        controller.SelectType(controller.TypeOptions.Single(option => option.Type == ModuleResourceType.Utp));
        Assert.AreEqual(PaletteSelection.ForType(ModuleResourceType.Utp), palette.Settings.Selection);

        controller.SelectMode(PaletteMode.Tiles);
        Assert.AreEqual(PaletteSelection.Tiles, palette.Settings.Selection);

        controller.SetTileSize(120);
        Assert.AreEqual(120, palette.Settings.PreviewSize);
    }

    [TestMethod]
    public void NothingSavedKeepsTheDefaultTypeAndUnofferedTypesAreIgnored()
    {
        using var palette = new PaletteWorkflowFixture();
        palette.Settings.Selection = PaletteSelection.ForType(ModuleResourceType.Uti);

        Assert.AreEqual(ModuleResourceType.Utp, palette.Controller.SelectedType);
        Assert.IsFalse(palette.Controller.IsTileMode);
        CollectionAssert.AreEqual(
            new[] { "Tiles", "Creatures", "Placeables" },
            palette.Controller.TypeOptions.Select(option => option.Label).ToArray(),
            "Tiles leads the type row, followed by the host's own order.");
        Assert.AreEqual("New Placeable...", palette.Controller.NewBlueprintLabel);
    }

    [TestMethod]
    public void TheWriteGateRepublishesCapabilitiesAndKeepsTheSelection()
    {
        using var palette = new PaletteWorkflowFixture();
        palette.Content.AddCustom(ModuleResourceType.Utp, "crate");
        palette.Controller.Refresh();
        palette.SelectRow("Unsorted");
        palette.State.SelectedTile = palette.Tile("crate");
        var announced = new List<string?>();
        palette.Controller.PropertyChanged += (_, e) => announced.Add(e.PropertyName);

        palette.WriteGate.Set(true);

        Assert.IsFalse(palette.Controller.CanWrite);
        CollectionAssert.Contains(announced, nameof(PaletteWorkflowController.CanWrite));
        Assert.AreEqual("Unsorted", palette.State.SelectedRow?.Name);
        Assert.AreEqual("crate", palette.State.SelectedTile?.ResRef);
        Assert.IsFalse(palette.State.SelectedTile!.Snapshot.Capabilities.CanDelete);
        Assert.IsFalse(palette.State.SelectedTile.Snapshot.Capabilities.CanEditCopy);

        palette.WriteGate.Set(false);

        Assert.IsTrue(palette.State.SelectedTile!.Snapshot.Capabilities.CanDelete);
    }

    [TestMethod]
    public void CategoryChangesFromTheHostRefreshThePalette()
    {
        using var palette = new PaletteWorkflowFixture();
        palette.Content.AddCustom(ModuleResourceType.Utp, "crate");
        palette.Controller.Refresh();
        palette.Categories.Live.Section(ModuleResourceType.Utp).AddFolder("Boxes").AddMember("crate");

        palette.Categories.SaveChanges();

        Assert.IsTrue(palette.State.Rows.Any(row => row.Name == "Boxes"));
    }

    private static StandardPalette StandardBoxes()
    {
        var section = new CategorySection();
        section.AddFolder("Containers").AddMember("std_box");
        return new StandardPalette(
            section,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "std_box" },
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["std_box"] = "Box" });
    }
}
