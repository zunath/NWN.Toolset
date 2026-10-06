using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Resources;
using Nwn.Toolset.Avalonia.Tests.Support;

namespace Nwn.Toolset.Avalonia.Tests.Explorer;

[TestClass]
public sealed class ModuleExplorerFolderTests
{
    [TestMethod]
    public async Task NewFolderGoesInsideTheSelectedFolder()
    {
        using var fixture = new ModuleExplorerFixture();
        var parent = fixture.SeededSection(ModuleResourceType.Nss).AddFolder("Parent");
        var controller = fixture.Open(ModuleResourceType.Nss);
        controller.SelectedRow = fixture.FolderRow(parent);
        fixture.Prompts.Answers.Enqueue("  Child  ");

        await controller.NewFolderCommand.ExecuteAsync(null);

        Assert.AreEqual("New folder in 'Parent'", fixture.Prompts.Headlines.Single());
        Assert.AreEqual("Child", parent.Children.Single().Name);
        Assert.AreEqual(1, fixture.Categories.Saves);
    }

    [TestMethod]
    public async Task ADuplicateOrMalformedFolderNameIsReportedRatherThanSanitized()
    {
        using var fixture = new ModuleExplorerFixture();
        var section = fixture.SeededSection(ModuleResourceType.Nss);
        section.AddFolder("Taken");
        var controller = fixture.Open(ModuleResourceType.Nss);

        fixture.Prompts.Answers.Enqueue("taken");
        await controller.NewFolderCommand.ExecuteAsync(null);
        Assert.AreEqual("A folder named 'taken' already exists here.", controller.StatusMessage);

        fixture.Prompts.Answers.Enqueue("a/b");
        await controller.NewFolderCommand.ExecuteAsync(null);
        Assert.IsFalse(string.IsNullOrWhiteSpace(controller.StatusMessage));
        Assert.AreEqual(1, section.Folders.Count);
    }

    [TestMethod]
    public async Task RenamingKeepsTheFolderSelectedAndForgetsMoveHistory()
    {
        using var fixture = new ModuleExplorerFixture();
        fixture.Content.Add(ModuleResourceType.Nss, "one");
        var section = fixture.SeededSection(ModuleResourceType.Nss);
        var first = section.AddFolder("First");
        var second = section.AddFolder("Second");
        first.AddMember("one");
        var controller = fixture.Open(ModuleResourceType.Nss);
        controller.ToggleCommand.Execute(fixture.FolderRow(first));
        controller.DropResource(fixture.Resource(fixture.FolderRow(first), "one"), fixture.FolderRow(second));
        Assert.IsTrue(controller.UndoResourceMoveCommand.CanExecute(null));

        controller.SelectedRow = fixture.FolderRow(second);
        fixture.Prompts.Answers.Enqueue("Renamed");
        await controller.RenameFolderCommand.ExecuteAsync(null);

        Assert.AreEqual("Renamed", second.Name);
        Assert.AreSame(second, controller.SelectedRow?.Folder);
        Assert.IsFalse(controller.UndoResourceMoveCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task DeletingAFolderConfirmsWhatItHoldsAndSendsMembersToUnsorted()
    {
        using var fixture = new ModuleExplorerFixture();
        fixture.Content.Add(ModuleResourceType.Nss, "one");
        var section = fixture.SeededSection(ModuleResourceType.Nss);
        var folder = section.AddFolder("Folder");
        folder.AddMember("one");
        folder.AddChild("Child");
        var controller = fixture.Open(ModuleResourceType.Nss);
        controller.SelectedRow = fixture.FolderRow(folder);

        fixture.Prompts.Confirms = false;
        await controller.DeleteFolderCommand.ExecuteAsync(null);
        Assert.AreEqual(1, section.Folders.Count);
        Assert.AreEqual(
            "1 sub-folder(s) are removed with it, 1 item(s) move back to Unsorted. Nothing is deleted from the module.",
            fixture.Prompts.Messages.Single());

        fixture.Prompts.Confirms = true;
        controller.SelectedRow = fixture.FolderRow(folder);
        await controller.DeleteFolderCommand.ExecuteAsync(null);

        Assert.AreEqual(0, section.Folders.Count);
        Assert.AreEqual(1, fixture.Row("Unsorted").Count);
        Assert.IsTrue(fixture.Content.Exists(ModuleResourceType.Nss, "one"));
    }

    [TestMethod]
    public async Task AnEmptyFolderIsDeletedWithoutAsking()
    {
        using var fixture = new ModuleExplorerFixture();
        var folder = fixture.SeededSection(ModuleResourceType.Nss).AddFolder("Empty");
        var controller = fixture.Open(ModuleResourceType.Nss);
        controller.SelectedRow = fixture.FolderRow(folder);

        await controller.DeleteFolderCommand.ExecuteAsync(null);

        Assert.AreEqual(0, fixture.Prompts.Headlines.Count);
        Assert.AreEqual(0, fixture.Section(ModuleResourceType.Nss).Folders.Count);
    }
}
