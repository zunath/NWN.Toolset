using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Resources;
using Nwn.Toolset.Avalonia.Tests.Support;

namespace Nwn.Toolset.Avalonia.Tests.Palettes.Workflow;

/// <summary>
/// The selected category survives a host replacing its tree - a refused save restoring the persisted copy,
/// a reload, a placeholder repair - because it is held by path, never by folder object.
/// </summary>
[TestClass]
public sealed class PaletteWorkflowSelectionTests
{
    [TestMethod]
    public async Task ARefusedRenameKeepsTheRestoredFolderSelected()
    {
        using var palette = new PaletteWorkflowFixture();
        palette.Categories.Live.Section(ModuleResourceType.Utp).AddFolder("Weapons");
        palette.Categories.Commit();
        palette.Controller.Refresh();
        var weapons = palette.SelectRow("Weapons");
        palette.Categories.Refusal = "read-only sidecar";
        palette.Prompts.Answers.Enqueue("Arms");

        await palette.Controller.RenameCategoryAsync(weapons.Id, CancellationToken.None);

        Assert.AreEqual("read-only sidecar", palette.Controller.StatusMessage);
        Assert.IsNotNull(palette.Categories.Live.Section(ModuleResourceType.Utp).Find("Weapons"));
        Assert.AreEqual("Weapons", palette.State.SelectedRow?.Name);
    }

    [TestMethod]
    public async Task ARefusedSubcategoryKeepsTheRestoredParentSelected()
    {
        using var palette = new PaletteWorkflowFixture();
        palette.Categories.Live.Section(ModuleResourceType.Utp).AddFolder("Weapons");
        palette.Categories.Commit();
        palette.Controller.Refresh();
        var weapons = palette.SelectRow("Weapons");
        palette.Categories.Refusal = "read-only sidecar";
        palette.Prompts.Answers.Enqueue("Rifles");

        await palette.Controller.NewCategoryAsync(weapons.Id, CancellationToken.None);

        Assert.IsNull(palette.Categories.Live.Section(ModuleResourceType.Utp).Find("Weapons", "Rifles"));
        Assert.AreEqual("Weapons", palette.State.SelectedRow?.Name);
    }

    [TestMethod]
    public void AReplacedTreeReselectsTheSameFolderByPath()
    {
        using var palette = new PaletteWorkflowFixture();
        palette.Content.AddCustom(ModuleResourceType.Utp, "crate");
        palette.Categories.Live.Section(ModuleResourceType.Utp).AddFolder("Furniture").AddChild("Boxes")
            .AddMember("crate");
        palette.Categories.Commit();
        palette.Controller.Refresh();
        palette.SelectRow("Boxes");

        palette.Categories.ReplaceLive();

        Assert.AreEqual("Boxes", palette.State.SelectedRow?.Name);
        palette.State.SelectedTile = palette.Tile("crate");
        palette.Controller.FileSelectedEntry(palette.State.SelectedRow!.Id);
        CollectionAssert.AreEqual(
            new[] { "crate" },
            palette.Categories.Live.Section(ModuleResourceType.Utp).Find("Furniture", "Boxes")!.Members.ToArray(),
            "Commands act on the folder in the current tree, not the replaced one.");
    }

    [TestMethod]
    public async Task AFolderGoneFromAReplacedTreeDropsTheSelection()
    {
        using var palette = new PaletteWorkflowFixture();
        palette.Categories.Live.Section(ModuleResourceType.Utp).AddFolder("Weapons");
        palette.Categories.Commit();
        palette.Controller.Refresh();
        palette.SelectRow("Weapons");

        palette.Categories.ReplaceLive(catalog =>
            catalog.Section(ModuleResourceType.Utp).RemoveFolder(catalog.Section(ModuleResourceType.Utp).Find("Weapons")!));

        Assert.IsNull(palette.State.SelectedRow);
        palette.Prompts.Answers.Enqueue("Armor");
        await palette.Controller.NewCategoryAsync(null, CancellationToken.None);
        Assert.IsNotNull(
            palette.Categories.Live.Section(ModuleResourceType.Utp).Find("Armor"),
            "With its folder gone the next category lands at the top level.");
    }

    [TestMethod]
    public async Task ATreeReplacedWhileThePromptIsOpenIsEditedByPath()
    {
        using var palette = new PaletteWorkflowFixture();
        palette.Categories.Live.Section(ModuleResourceType.Utp).AddFolder("Weapons");
        palette.Categories.Commit();
        palette.Controller.Refresh();
        var weapons = palette.SelectRow("Weapons");
        palette.Prompts.Answers.Enqueue("Rifles");
        palette.Prompts.DuringPrompt = () => palette.Categories.ReplaceLive();

        await palette.Controller.NewCategoryAsync(weapons.Id, CancellationToken.None);

        Assert.IsNotNull(palette.Categories.Live.Section(ModuleResourceType.Utp).Find("Weapons", "Rifles"));
        Assert.AreEqual("Added category 'Rifles'.", palette.Controller.StatusMessage);
    }
}
