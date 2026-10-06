using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Resources;
using Nwn.Toolset.Avalonia.Tests.Support;

namespace Nwn.Toolset.Avalonia.Tests.Palettes.Workflow;

[TestClass]
public sealed class PaletteWorkflowCategoryTests
{
    [TestMethod]
    public async Task NewCategoriesAreAddedTopLevelOrInsideTheSelectionAndSaved()
    {
        using var palette = new PaletteWorkflowFixture();
        palette.Controller.Refresh();
        palette.Prompts.Answers.Enqueue("Weapons");
        palette.Prompts.Answers.Enqueue("Rifles");

        await palette.Controller.NewCategoryAsync(null, CancellationToken.None);
        var weapons = palette.SelectRow("Weapons");
        await palette.Controller.NewCategoryAsync(weapons.Id, CancellationToken.None);

        var section = palette.Categories.Live.Section(ModuleResourceType.Utp);
        Assert.IsNotNull(section.Find("Weapons", "Rifles"));
        Assert.AreEqual(2, palette.Categories.Saves);
        Assert.AreEqual("Added category 'Rifles'.", palette.Controller.StatusMessage);
        Assert.AreEqual("New category inside 'Weapons'", palette.Prompts.Headlines[1]);
        CollectionAssert.Contains(palette.Log.Lines, "Added category 'Rifles' to the placeables palette.");
    }

    [TestMethod]
    public async Task DuplicateAndInvalidNamesAreReportedWithoutSaving()
    {
        using var palette = new PaletteWorkflowFixture();
        palette.Categories.Live.Section(ModuleResourceType.Utp).AddFolder("Weapons");
        palette.Categories.Commit();
        palette.Controller.Refresh();
        palette.Prompts.Answers.Enqueue(" weapons ");
        palette.Prompts.Answers.Enqueue("A/B");

        await palette.Controller.NewCategoryAsync(null, CancellationToken.None);
        Assert.AreEqual("A category named 'weapons' already exists here.", palette.Controller.StatusMessage);

        await palette.Controller.NewCategoryAsync(null, CancellationToken.None);
        Assert.IsNotNull(palette.Controller.StatusMessage);
        Assert.AreNotEqual("A category named 'weapons' already exists here.", palette.Controller.StatusMessage);
        Assert.AreEqual(0, palette.Categories.Saves);
    }

    [TestMethod]
    public async Task RenameMovesThePinAndKeepsTheFolderSelected()
    {
        using var palette = new PaletteWorkflowFixture();
        var section = palette.Categories.Live.Section(ModuleResourceType.Utp);
        section.AddFolder("Weapons");
        section.Pin("Weapons");
        palette.Categories.Commit();
        palette.Controller.Refresh();
        palette.Prompts.Answers.Enqueue("Arms");

        var weapons = palette.SelectRow("Weapons");
        await palette.Controller.RenameCategoryAsync(weapons.Id, CancellationToken.None);

        CollectionAssert.AreEqual(new[] { "Arms" }, palette.Categories.Live.Section(ModuleResourceType.Utp).Pinned.ToArray());
        Assert.AreEqual("Renamed 'Weapons' to 'Arms'.", palette.Controller.StatusMessage);
        Assert.AreEqual("Arms", palette.State.SelectedRow?.Name);
    }

    [TestMethod]
    public async Task DeleteRefusesCategoriesWithContentsAndRemovesConfirmedEmptyOnes()
    {
        using var palette = new PaletteWorkflowFixture();
        palette.Content.AddCustom(ModuleResourceType.Utp, "crate");
        var section = palette.Categories.Live.Section(ModuleResourceType.Utp);
        section.AddFolder("Full").AddMember("crate");
        section.AddFolder("Parent").AddChild("Child");
        section.AddFolder("Empty");
        palette.Categories.Commit();
        palette.Controller.Refresh();

        await palette.Controller.DeleteCategoryAsync(palette.SelectRow("Full").Id, CancellationToken.None);
        Assert.AreEqual("'Full' still holds blueprints - empty it first.", palette.Controller.StatusMessage);

        await palette.Controller.DeleteCategoryAsync(palette.SelectRow("Parent").Id, CancellationToken.None);
        Assert.AreEqual("'Parent' still holds sub-categories - remove them first.", palette.Controller.StatusMessage);

        await palette.Controller.DeleteCategoryAsync(palette.SelectRow("Empty").Id, CancellationToken.None);
        Assert.AreEqual("Removed category 'Empty'.", palette.Controller.StatusMessage);
        Assert.IsNull(palette.Categories.Live.Section(ModuleResourceType.Utp).Find("Empty"));
        Assert.AreEqual(1, palette.Prompts.Headlines.Count, "Only the deletable category asked for confirmation.");
    }

    [TestMethod]
    public async Task PinsToggleByPath()
    {
        using var palette = new PaletteWorkflowFixture();
        var section = palette.Categories.Live.Section(ModuleResourceType.Utp);
        section.AddFolder("A").AddChild("Shared");
        section.AddFolder("B").AddChild("Shared");
        palette.Categories.Commit();
        palette.Controller.Refresh();

        var nested = palette.SelectRow("Shared");
        await palette.Controller.TogglePinAsync(nested.Id, CancellationToken.None);
        CollectionAssert.AreEqual(new[] { "A/Shared" }, palette.Categories.Live.Section(ModuleResourceType.Utp).Pinned.ToArray());

        await palette.Controller.TogglePinAsync(nested.Id, CancellationToken.None);
        Assert.AreEqual(0, palette.Categories.Live.Section(ModuleResourceType.Utp).Pinned.Count);
    }

    [TestMethod]
    public void FilingMovesTheSelectedBlueprintOutOfEveryOtherCategory()
    {
        using var palette = new PaletteWorkflowFixture();
        palette.Content.AddCustom(ModuleResourceType.Utp, "crate", "Crate");
        var section = palette.Categories.Live.Section(ModuleResourceType.Utp);
        section.AddFolder("Old").AddMember("crate");
        section.AddFolder("New");
        palette.Categories.Commit();
        palette.Controller.Refresh();
        palette.SelectRow("Old");
        palette.State.SelectedTile = palette.Tile("crate");

        palette.Controller.FileSelectedEntry(palette.State.Rows.Single(row => row.Name == "New").Id);

        var saved = palette.Categories.Live.Section(ModuleResourceType.Utp);
        CollectionAssert.AreEqual(new[] { "crate" }, saved.Find("New")!.Members.ToArray());
        Assert.AreEqual(0, saved.Find("Old")!.Members.Count);
        Assert.AreEqual("Filed Crate into 'New'.", palette.Controller.StatusMessage);
    }

    [TestMethod]
    public async Task ARefusedSaveIsReportedAndTheEditIsRolledBack()
    {
        using var palette = new PaletteWorkflowFixture();
        palette.Controller.Refresh();
        palette.Categories.Refusal = "The sidecar changed outside the toolset.";
        palette.Prompts.Answers.Enqueue("Weapons");

        await palette.Controller.NewCategoryAsync(null, CancellationToken.None);

        Assert.AreEqual("The sidecar changed outside the toolset.", palette.Controller.StatusMessage);
        Assert.IsNull(palette.Categories.Live.Section(ModuleResourceType.Utp).Find("Weapons"));
        Assert.IsFalse(palette.State.Rows.Any(row => row.Name == "Weapons"));
    }
}
