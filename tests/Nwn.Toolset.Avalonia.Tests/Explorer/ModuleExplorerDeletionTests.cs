using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Resources;
using Nwn.Toolset.Avalonia.Explorer.Workflow;
using Nwn.Toolset.Avalonia.Tests.Support;

namespace Nwn.Toolset.Avalonia.Tests.Explorer;

[TestClass]
public sealed class ModuleExplorerDeletionTests
{
    private static (ModuleExplorerFixture Fixture, ModuleExplorerController Controller) Selected(
        ModuleResourceType type, string resRef, string? folderName = null)
    {
        var fixture = new ModuleExplorerFixture();
        fixture.Content.Add(type, resRef);
        var section = fixture.SeededSection(type);
        if (folderName != null)
        {
            section.AddFolder(folderName).AddMember(resRef);
            fixture.Categories.Commit();
        }

        var controller = fixture.Open(type);
        var parent = folderName == null ? fixture.Row("Unsorted") : fixture.Row(folderName);
        controller.ToggleCommand.Execute(parent);
        controller.SelectedRow = controller.Rows.Single(row => row.ResRef == resRef);
        return (fixture, controller);
    }

    [TestMethod]
    public async Task AConfirmedDeleteRemovesTheResourceAndItsFolderMembership()
    {
        var (fixture, controller) = Selected(ModuleResourceType.Nss, "doomed", "Folder");
        using var _ = fixture;

        await controller.DeleteSelectedResourceCommand.ExecuteAsync(null);

        Assert.AreEqual("Delete 'doomed'?", fixture.Prompts.Headlines.Single());
        Assert.AreEqual("This cannot be undone.", fixture.Prompts.Messages.Single());
        Assert.IsFalse(fixture.Content.Exists(ModuleResourceType.Nss, "doomed"));
        Assert.IsFalse(fixture.Section(ModuleResourceType.Nss).FoldersContaining("doomed").Any());
        Assert.AreEqual((ModuleResourceType.Nss, "doomed"), fixture.Deletion.DeletedNotices.Single());
        Assert.AreEqual(string.Empty, controller.StatusMessage);
        Assert.AreEqual("Deleted script 'doomed' (doomed.file).", fixture.Log.Lines.Single());
        Assert.IsNull(controller.SelectedRow);
        Assert.AreEqual(0, fixture.Deletion.ActiveReservations);
    }

    [TestMethod]
    public async Task DecliningTheConfirmationDeletesNothing()
    {
        var (fixture, controller) = Selected(ModuleResourceType.Dlg, "kept");
        using var _ = fixture;
        fixture.Prompts.Confirms = false;

        await controller.DeleteSelectedResourceCommand.ExecuteAsync(null);

        Assert.IsTrue(fixture.Content.Exists(ModuleResourceType.Dlg, "kept"));
        Assert.AreEqual(0, fixture.Deletion.DeletedNotices.Count);
    }

    [TestMethod]
    public async Task AnOpenEditorIsWarnedAboutAndClosedOnlyAfterTheCommit()
    {
        var (fixture, controller) = Selected(ModuleResourceType.Nss, "open_one");
        using var _ = fixture;
        fixture.Editors.OpenEditors.Add((ModuleResourceType.Nss, "open_one"));

        await controller.DeleteSelectedResourceCommand.ExecuteAsync(null);

        Assert.AreEqual(
            "Unsaved changes in the open editor will be lost. This cannot be undone.",
            fixture.Prompts.Messages.Single());
        Assert.AreEqual((ModuleResourceType.Nss, "open_one"), fixture.Editors.Closed.Single());
    }

    [TestMethod]
    public async Task AnEditorOpenedDuringTheConfirmationCancelsTheDelete()
    {
        var (fixture, controller) = Selected(ModuleResourceType.Nss, "racing");
        using var _ = fixture;
        fixture.Prompts.DuringConfirmation = () => fixture.Editors.OpenEditors.Add((ModuleResourceType.Nss, "racing"));

        await controller.DeleteSelectedResourceCommand.ExecuteAsync(null);

        Assert.IsTrue(fixture.Content.Exists(ModuleResourceType.Nss, "racing"));
        StringAssert.Contains(controller.StatusMessage, "opened while the delete confirmation was active");
        Assert.AreEqual(0, fixture.Editors.Closed.Count);
    }

    [TestMethod]
    public async Task ARefusedReservationOrPreparationLeavesTheResource()
    {
        var (fixture, controller) = Selected(ModuleResourceType.Nss, "locked");
        using var _ = fixture;

        fixture.Deletion.ReservationRefused = true;
        await controller.DeleteSelectedResourceCommand.ExecuteAsync(null);
        Assert.AreEqual("'locked' was not deleted: the module is being packed, validated, or built.", controller.StatusMessage);

        fixture.Deletion.ReservationRefused = false;
        fixture.Deletion.PrepareRefusal = "it is the area template";
        await controller.DeleteSelectedResourceCommand.ExecuteAsync(null);
        Assert.AreEqual("'locked' was not deleted: it is the area template", controller.StatusMessage);
        Assert.AreEqual("Deleting script 'locked' was refused: it is the area template", fixture.Log.Lines.Last());

        Assert.IsTrue(fixture.Content.Exists(ModuleResourceType.Nss, "locked"));
    }

    [TestMethod]
    public async Task AFailedCommitIsReportedAndReleasesTheReservation()
    {
        var (fixture, controller) = Selected(ModuleResourceType.Nss, "changed");
        using var _ = fixture;
        fixture.Deletion.CommitFailure = "it changed while the confirmation was open";

        await controller.DeleteSelectedResourceCommand.ExecuteAsync(null);

        Assert.AreEqual("'changed' was not deleted: it changed while the confirmation was open", controller.StatusMessage);
        Assert.IsFalse(controller.IsDeletingResource);
        Assert.AreEqual(0, fixture.Deletion.ActiveReservations);
    }

    [TestMethod]
    public async Task AFiledResourceIsNotDeletedWhenTheSidecarCannotBeUpdated()
    {
        var (fixture, controller) = Selected(ModuleResourceType.Nss, "filed", "Folder");
        using var _ = fixture;
        fixture.Categories.Refusal = "read-only";

        await controller.DeleteSelectedResourceCommand.ExecuteAsync(null);

        Assert.AreEqual("'filed' was not deleted: read-only", controller.StatusMessage);
        Assert.AreEqual(0, fixture.Prompts.Headlines.Count, "refused before asking");
        Assert.IsTrue(fixture.Content.Exists(ModuleResourceType.Nss, "filed"));
    }

    [TestMethod]
    public async Task AreasCannotBeDeletedWhileModulePropertiesIsOpen()
    {
        var (fixture, controller) = Selected(ModuleResourceType.Area, "area_one");
        using var _ = fixture;

        fixture.Editors.IsModulePropertiesOpen = true;
        await controller.DeleteSelectedResourceCommand.ExecuteAsync(null);
        Assert.AreEqual("Module Properties is open - close that tab before deleting an area.", controller.StatusMessage);

        fixture.Editors.IsModulePropertiesOpen = false;
        fixture.Prompts.DuringConfirmation = () => fixture.Editors.IsModulePropertiesOpen = true;
        await controller.DeleteSelectedResourceCommand.ExecuteAsync(null);
        Assert.AreEqual("Module Properties is now open - close that tab before deleting an area.", controller.StatusMessage);

        Assert.IsTrue(fixture.Content.Exists(ModuleResourceType.Area, "area_one"));
    }

    [TestMethod]
    public async Task CleanupWarningsAreReportedAfterACommittedDelete()
    {
        var (fixture, controller) = Selected(ModuleResourceType.Nss, "messy");
        using var _ = fixture;
        fixture.Deletion.CleanupWarnings = new[] { "backup a", "backup b" };

        await controller.DeleteSelectedResourceCommand.ExecuteAsync(null);

        Assert.AreEqual("Temporary delete backup cleanup needs attention: backup a; backup b", controller.StatusMessage);
    }

    [TestMethod]
    public void DeletingIsUnavailableForFoldersAndWhileLocked()
    {
        var (fixture, controller) = Selected(ModuleResourceType.Nss, "one");
        using var _ = fixture;
        Assert.IsTrue(controller.DeleteSelectedResourceCommand.CanExecute(null));

        fixture.WriteGate.Set(true);
        Assert.IsFalse(controller.DeleteSelectedResourceCommand.CanExecute(null));

        fixture.WriteGate.Set(false);
        controller.SelectedRow = fixture.Row("Unsorted");
        Assert.IsFalse(controller.CanDeleteSelectedResource);
    }
}
