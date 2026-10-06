using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Resources;
using Nwn.Toolset.Avalonia.Palettes.Workflow;
using Nwn.Toolset.Avalonia.Tests.Support;

namespace Nwn.Toolset.Avalonia.Tests.Palettes.Workflow;

/// <summary>A host's own creation UI replaces the name prompt for the types it handles.</summary>
[TestClass]
public sealed class PaletteWorkflowCreationDialogTests
{
    [TestMethod]
    public async Task TheHostDialogRunsInsteadOfTheNamePromptAndItsBlueprintIsFiled()
    {
        using var palette = new PaletteWorkflowFixture();
        palette.CreationDialog = new FakePaletteBlueprintCreationDialog(palette.Content)
        {
            Result = PaletteBlueprintCreation.CreatedByHost("door_01", "Iron Door", "module/door_01", openedInEditor: true)
        };
        palette.Categories.Live.Section(ModuleResourceType.Utp).AddFolder("Doors");
        palette.Categories.Commit();
        palette.Controller.Refresh();
        var doors = palette.SelectRow("Doors");

        palette.Controller.NewBlueprint(doors.Id);
        await palette.Controller.NewBlueprintCommand.ExecutionTask!;

        Assert.AreEqual(ModuleResourceType.Utp, palette.CreationDialog.Shown.Single());
        Assert.AreEqual(0, palette.Prompts.Headlines.Count, "No name prompt.");
        Assert.AreEqual(0, palette.Blueprints.Created.Count, "The dialog wrote it, not CreateAsync.");
        CollectionAssert.AreEqual(
            new[] { "door_01" },
            palette.Categories.Live.Section(ModuleResourceType.Utp).Find("Doors")!.Members.ToArray());
        Assert.AreEqual("Created Iron Door.", palette.Controller.StatusMessage);
        CollectionAssert.Contains(palette.Log.Lines, "Created utp blueprint 'door_01' (module/door_01).");
        Assert.AreEqual(0, palette.Blueprints.OpenedEditors.Count, "The dialog already opened its editor.");
    }

    [TestMethod]
    public async Task ADialogThatLeavesTheEditorClosedHasThePaletteOpenIt()
    {
        using var palette = new PaletteWorkflowFixture();
        palette.CreationDialog = new FakePaletteBlueprintCreationDialog(palette.Content)
        {
            Result = PaletteBlueprintCreation.CreatedByHost("door_01", null, "module/door_01", openedInEditor: false)
        };
        palette.Controller.Refresh();

        await palette.Controller.NewBlueprintCommand.ExecuteAsync(null);

        Assert.AreEqual("Created door_01.", palette.Controller.StatusMessage);
        CollectionAssert.AreEqual(new[] { "door_01" }, palette.Blueprints.OpenedEditors);
    }

    [TestMethod]
    public async Task ACancelledDialogChangesNothing()
    {
        using var palette = new PaletteWorkflowFixture();
        palette.CreationDialog = new FakePaletteBlueprintCreationDialog(palette.Content);
        palette.Controller.Refresh();

        await palette.Controller.NewBlueprintCommand.ExecuteAsync(null);

        Assert.AreEqual(1, palette.CreationDialog.Shown.Count);
        Assert.IsNull(palette.Controller.StatusMessage);
        Assert.AreEqual(0, palette.Categories.Saves);
        Assert.AreEqual(0, palette.Blueprints.OpenedEditors.Count);
    }

    [TestMethod]
    public async Task TypesTheDialogDoesNotHandleKeepTheNamePrompt()
    {
        using var palette = new PaletteWorkflowFixture();
        palette.CreationDialog = new FakePaletteBlueprintCreationDialog(palette.Content);
        palette.Controller.SelectedType = ModuleResourceType.Utc;
        palette.Prompts.Answers.Enqueue("Guard");

        await palette.Controller.NewBlueprintCommand.ExecuteAsync(null);

        Assert.AreEqual(0, palette.CreationDialog.Shown.Count);
        Assert.AreEqual((ModuleResourceType.Utc, "guard", "Guard"), palette.Blueprints.Created.Single());
    }
}
