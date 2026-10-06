using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Categories;
using Nwn.Authoring.Resources;
using Nwn.Toolset.Avalonia.Palettes;
using Nwn.Toolset.Avalonia.Tests.Support;

namespace Nwn.Toolset.Avalonia.Tests.Palettes.Workflow;

[TestClass]
public sealed class PaletteWorkflowEntryTests
{
    [TestMethod]
    public void PlacingArmsTheAreaInFrontOrAsksForOne()
    {
        using var palette = new PaletteWorkflowFixture();
        palette.Content.AddCustom(ModuleResourceType.Utp, "crate", "Crate");
        var target = new FakePalettePlacementTarget();
        palette.Placement.ActiveTarget = target;
        palette.Controller.Refresh();
        palette.SelectRow("Unsorted");

        palette.Controller.Place(palette.Tile("crate").Snapshot);

        Assert.AreEqual((ModuleResourceType.Utp, "crate", PaletteSource.Custom), target.Armed.Single());
        Assert.AreEqual("Click the map to place Crate.", palette.Controller.StatusMessage);

        target.Accepts = false;
        palette.Controller.Place(palette.Tile("crate").Snapshot);
        Assert.AreEqual("Placeables cannot be placed in this area.", palette.Controller.StatusMessage);

        palette.Placement.ActiveTarget = null;
        palette.Controller.Place(palette.Tile("crate").Snapshot);
        Assert.AreEqual("Open an area first, then place into it.", palette.Controller.StatusMessage);
    }

    [TestMethod]
    public void EditCopyFilesTheCopyUnderTheMatchingCustomCategoryAndRevealsIt()
    {
        using var palette = new PaletteWorkflowFixture();
        var standard = new CategorySection();
        standard.AddFolder("Containers").AddChild("Boxes").AddMember("std_box");
        palette.Content.StandardPalettes[ModuleResourceType.Utp] = new StandardPalette(
            standard,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "std_box" },
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["std_box"] = "Box" });
        palette.Controller.SelectSource(PaletteSource.Standard);
        palette.SelectRow("Boxes");

        palette.Controller.EditCopy(palette.Tile("std_box").Snapshot);

        Assert.AreEqual((PaletteSource.Standard, "std_box"), palette.Blueprints.Copied.Single());
        CollectionAssert.AreEqual(
            new[] { "std_box001" },
            palette.Categories.Live.Section(ModuleResourceType.Utp).Find("Containers", "Boxes")!.Members.ToArray());
        Assert.AreEqual(PaletteSource.Custom, palette.Controller.Source, "Edit Copy always lands on the Custom side.");
        Assert.AreEqual("Boxes", palette.State.SelectedRow?.Name);
        Assert.AreEqual("std_box001", palette.State.SelectedTile?.ResRef);
        Assert.AreEqual("Copied Box as std_box001.", palette.Controller.StatusMessage);
        CollectionAssert.AreEqual(new[] { "std_box001" }, palette.Blueprints.OpenedEditors);
    }

    [TestMethod]
    public async Task DeletingConfirmsCommitsUnfilesAndReleasesTheDeletion()
    {
        using var palette = new PaletteWorkflowFixture();
        palette.Content.AddCustom(ModuleResourceType.Utp, "crate", "Crate");
        palette.Categories.Live.Section(ModuleResourceType.Utp).AddFolder("Boxes").AddMember("crate");
        palette.Categories.Commit();
        palette.Controller.Refresh();
        palette.SelectRow("Boxes");

        await palette.Controller.DeleteAsync(palette.Tile("crate").Snapshot, CancellationToken.None);

        var deletion = palette.Blueprints.Deletions.Single();
        Assert.IsTrue(deletion.Committed);
        Assert.IsTrue(deletion.Disposed);
        Assert.AreEqual("Delete the placeable 'Crate'?", palette.Prompts.Headlines.Single());
        StringAssert.StartsWith(palette.Prompts.Messages.Single(), "This deletes crate.file from the module.");
        Assert.AreEqual(0, palette.Categories.Live.Section(ModuleResourceType.Utp).Find("Boxes")!.Members.Count);
        Assert.AreEqual("Deleted Crate.", palette.Controller.StatusMessage);
        CollectionAssert.Contains(palette.Log.Lines, "Deleted blueprint 'crate' (module/crate).");
    }

    [TestMethod]
    public async Task DeletingIsRefusedWhenTheModuleLocksWhileTheConfirmationIsOpen()
    {
        using var palette = new PaletteWorkflowFixture();
        palette.Content.AddCustom(ModuleResourceType.Utp, "crate", "Crate");
        palette.Controller.Refresh();
        palette.SelectRow("Unsorted");
        palette.Prompts.DuringConfirmation = () => palette.WriteGate.Set(true);

        await palette.Controller.DeleteAsync(palette.Tile("crate").Snapshot, CancellationToken.None);

        Assert.IsFalse(palette.Blueprints.Deletions.Single().Committed);
        Assert.AreEqual(
            "'Crate' was not deleted: the module is being packed, validated, or built.",
            palette.Controller.StatusMessage);
    }

    [TestMethod]
    public async Task DeletingIsRefusedForAnOpenEditorAndForASidecarThatCannotBeSaved()
    {
        using var palette = new PaletteWorkflowFixture();
        palette.Content.AddCustom(ModuleResourceType.Utp, "crate", "Crate");
        palette.Categories.Live.Section(ModuleResourceType.Utp).AddFolder("Boxes").AddMember("crate");
        palette.Categories.Commit();
        palette.Controller.Refresh();
        palette.SelectRow("Boxes");
        var crate = palette.Tile("crate").Snapshot;

        palette.Blueprints.OpenInEditor.Add("crate");
        await palette.Controller.DeleteAsync(crate, CancellationToken.None);
        Assert.AreEqual("'Crate' is open in an editor - close that tab first.", palette.Controller.StatusMessage);

        palette.Blueprints.OpenInEditor.Clear();
        palette.Categories.Refusal = "read-only sidecar";
        await palette.Controller.DeleteAsync(palette.Tile("crate").Snapshot, CancellationToken.None);
        Assert.AreEqual("'Crate' was not deleted: read-only sidecar", palette.Controller.StatusMessage);
        Assert.AreEqual(0, palette.Blueprints.Deletions.Count);
        Assert.AreEqual(0, palette.Prompts.Headlines.Count);
    }

    [TestMethod]
    public async Task RetainedEntriesCannotActAfterTheTypeOrSourceChanges()
    {
        using var palette = new PaletteWorkflowFixture();
        palette.Content.AddCustom(ModuleResourceType.Utp, "crate", "Crate");
        var target = new FakePalettePlacementTarget();
        palette.Placement.ActiveTarget = target;
        palette.Controller.Refresh();
        palette.SelectRow("Unsorted");
        var retained = palette.Tile("crate").Snapshot;

        palette.Controller.SelectedType = ModuleResourceType.Utc;
        await ActOn(palette, retained);

        palette.Controller.SelectedType = ModuleResourceType.Utp;
        palette.Controller.Source = PaletteSource.Standard;
        await ActOn(palette, retained);

        Assert.AreEqual(0, target.Armed.Count);
        Assert.AreEqual(0, palette.Blueprints.OpenedEditors.Count);
        Assert.AreEqual(0, palette.Blueprints.Copied.Count);
        Assert.AreEqual(0, palette.Blueprints.Deletions.Count);
        Assert.AreEqual(0, palette.Prompts.Headlines.Count);
    }

    [TestMethod]
    public async Task NewBlueprintDerivesItsResRefFilesItAndOpensIt()
    {
        using var palette = new PaletteWorkflowFixture();
        palette.Categories.Live.Section(ModuleResourceType.Utp).AddFolder("Boxes");
        palette.Categories.Commit();
        palette.Controller.Refresh();
        var boxes = palette.SelectRow("Boxes");
        palette.Prompts.Answers.Enqueue("Big Crate!");

        palette.Controller.NewBlueprint(boxes.Id);
        await palette.Controller.NewBlueprintCommand.ExecutionTask!;

        Assert.AreEqual((ModuleResourceType.Utp, "big_crate", "Big Crate!"), palette.Blueprints.Created.Single());
        CollectionAssert.AreEqual(
            new[] { "big_crate" },
            palette.Categories.Live.Section(ModuleResourceType.Utp).Find("Boxes")!.Members.ToArray());
        Assert.AreEqual("Created Big Crate!.", palette.Controller.StatusMessage);
        CollectionAssert.AreEqual(new[] { "big_crate" }, palette.Blueprints.OpenedEditors);
    }

    [TestMethod]
    public async Task NewBlueprintReportsANameWithNoResRefAndAnExistingResRef()
    {
        using var palette = new PaletteWorkflowFixture();
        palette.Content.AddCustom(ModuleResourceType.Utp, "crate");
        palette.Controller.Refresh();
        palette.Prompts.Answers.Enqueue("!!!");
        palette.Prompts.Answers.Enqueue("Crate");

        await palette.Controller.NewBlueprintCommand.ExecuteAsync(null);
        Assert.AreEqual("That name has no letters or digits to build a ResRef from.", palette.Controller.StatusMessage);

        await palette.Controller.NewBlueprintCommand.ExecuteAsync(null);
        Assert.AreEqual("A placeable called 'crate' already exists.", palette.Controller.StatusMessage);
        Assert.AreEqual(0, palette.Blueprints.Created.Count);
    }

    [TestMethod]
    public void HostsThatCannotDeleteOfferNoDeleteAction()
    {
        using var palette = new PaletteWorkflowFixture();
        palette.Content.AddCustom(ModuleResourceType.Utp, "crate");
        palette.Blueprints.Deletable = false;
        palette.Controller.Refresh();
        palette.SelectRow("Unsorted");

        var capabilities = palette.Tile("crate").Snapshot.Capabilities;
        Assert.IsFalse(capabilities.CanDelete);
        Assert.IsTrue(capabilities.CanEdit);
    }

    private static async Task ActOn(PaletteWorkflowFixture palette, PaletteEntrySnapshot retained)
    {
        palette.Controller.Place(retained);
        palette.Controller.Edit(retained);
        palette.Controller.EditCopy(retained);
        await palette.Controller.DeleteAsync(retained, CancellationToken.None);
    }
}
