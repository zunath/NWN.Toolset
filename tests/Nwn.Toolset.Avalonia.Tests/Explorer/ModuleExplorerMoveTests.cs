using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Resources;
using Nwn.Toolset.Avalonia.Tests.Support;

namespace Nwn.Toolset.Avalonia.Tests.Explorer;

[TestClass]
public sealed class ModuleExplorerMoveTests
{
    [TestMethod]
    [DataRow(ModuleResourceType.Area)]
    [DataRow(ModuleResourceType.Dlg)]
    [DataRow(ModuleResourceType.Nss)]
    public void AResourceCanBeDraggedIntoAndBackOutOfFoldersWithUndoAndRedo(ModuleResourceType type)
    {
        using var fixture = new ModuleExplorerFixture();
        fixture.Content.Add(type, "resource_one");
        var section = fixture.SeededSection(type);
        var first = section.AddFolder("First");
        var second = section.AddFolder("Second");
        first.AddMember("resource_one");
        var controller = fixture.Open(type);

        var source = fixture.Resource(fixture.FolderRow(first), "resource_one");
        Assert.IsTrue(controller.CanDropResource(source, fixture.FolderRow(second)));
        Assert.IsFalse(controller.CanDropResource(source, fixture.FolderRow(first)), "already its only folder");
        Assert.IsTrue(controller.DropResource(source, fixture.FolderRow(second)));
        Assert.AreEqual("Moved 'resource_one' to 'Second'.", controller.StatusMessage);
        Assert.IsTrue(second.Members.Contains("resource_one"));

        controller.UndoResourceMoveCommand.Execute(null);
        Assert.IsTrue(first.Members.Contains("resource_one"));
        Assert.IsFalse(second.Members.Contains("resource_one"));
        Assert.AreEqual("Undid move of 'resource_one'.", controller.StatusMessage);

        controller.RedoResourceMoveCommand.Execute(null);
        Assert.IsTrue(second.Members.Contains("resource_one"));

        source = fixture.Resource(fixture.FolderRow(second), "resource_one");
        Assert.IsTrue(controller.DropResource(source, fixture.Row("Unsorted")));
        Assert.IsFalse(section.FoldersContaining("resource_one").Any());
        Assert.AreEqual("Moved 'resource_one' to Unsorted.", controller.StatusMessage);

        controller.UndoResourceMoveCommand.Execute(null);
        Assert.IsTrue(second.Members.Contains("resource_one"));
    }

    [TestMethod]
    public void UndoRestoresEveryPreviousFolderMembership()
    {
        using var fixture = new ModuleExplorerFixture();
        fixture.Content.Add(ModuleResourceType.Nss, "one");
        var section = fixture.SeededSection(ModuleResourceType.Nss);
        var first = section.AddFolder("First");
        var second = section.AddFolder("Second");
        var third = section.AddFolder("Third");
        first.AddMember("one");
        third.AddMember("one");
        var controller = fixture.Open(ModuleResourceType.Nss);

        controller.DropResource(fixture.Resource(fixture.FolderRow(first), "one"), fixture.FolderRow(second));
        controller.UndoResourceMoveCommand.Execute(null);

        CollectionAssert.AreEqual(new[] { "First", "Third" }, section.FoldersContaining("one").Select(folder => folder.Name).ToArray());
    }

    [TestMethod]
    public void ARefusedSaveKeepsTheEditForARetry()
    {
        using var fixture = new ModuleExplorerFixture();
        fixture.Content.Add(ModuleResourceType.Nss, "one");
        var section = fixture.SeededSection(ModuleResourceType.Nss);
        var first = section.AddFolder("First");
        var second = section.AddFolder("Second");
        first.AddMember("one");
        fixture.Categories.Commit();
        var controller = fixture.Open(ModuleResourceType.Nss);
        controller.DropResource(fixture.Resource(fixture.FolderRow(first), "one"), fixture.FolderRow(second));

        fixture.Categories.Refusal = "The sidecar is read-only.";
        controller.UndoResourceMoveCommand.Execute(null);

        Assert.AreEqual("The sidecar is read-only.", controller.StatusMessage);
        Assert.IsTrue(controller.UndoResourceMoveCommand.CanExecute(null));

        fixture.Categories.Refusal = null;
        controller.UndoResourceMoveCommand.Execute(null);

        Assert.IsFalse(controller.UndoResourceMoveCommand.CanExecute(null));
        Assert.IsTrue(controller.RedoResourceMoveCommand.CanExecute(null));
        Assert.AreEqual("First", fixture.Section(ModuleResourceType.Nss).FoldersContaining("one").Single().Name);
    }

    [TestMethod]
    public void AMovedResourceThatDisappearsInvalidatesTheHistory()
    {
        using var fixture = new ModuleExplorerFixture();
        fixture.Content.Add(ModuleResourceType.Nss, "one");
        var section = fixture.SeededSection(ModuleResourceType.Nss);
        var first = section.AddFolder("First");
        var second = section.AddFolder("Second");
        first.AddMember("one");
        var controller = fixture.Open(ModuleResourceType.Nss);
        controller.DropResource(fixture.Resource(fixture.FolderRow(first), "one"), fixture.FolderRow(second));

        fixture.Content.RaiseResourceChanged(ModuleResourceType.Nss, "one");
        Assert.IsTrue(controller.UndoResourceMoveCommand.CanExecute(null), "a save of a still-existing resource keeps history");

        fixture.Content.Remove(ModuleResourceType.Nss, "one");
        fixture.Content.RaiseResourceChanged(ModuleResourceType.Nss, "one");

        Assert.IsFalse(controller.UndoResourceMoveCommand.CanExecute(null));
    }

    [TestMethod]
    public void UndoIntoAFolderThatWasDeletedIsRefusedAndClearsHistory()
    {
        using var fixture = new ModuleExplorerFixture();
        fixture.Content.Add(ModuleResourceType.Nss, "one");
        var section = fixture.SeededSection(ModuleResourceType.Nss);
        var first = section.AddFolder("First");
        var second = section.AddFolder("Second");
        first.AddMember("one");
        var controller = fixture.Open(ModuleResourceType.Nss);
        controller.DropResource(fixture.Resource(fixture.FolderRow(first), "one"), fixture.FolderRow(second));
        section.RemoveFolder(first);

        controller.UndoResourceMoveCommand.Execute(null);

        Assert.AreEqual("Cannot undo the move because folder 'First' no longer exists.", controller.StatusMessage);
        Assert.IsFalse(controller.UndoResourceMoveCommand.CanExecute(null));
        Assert.IsTrue(second.Members.Contains("one"));
    }

    [TestMethod]
    public void MoveToSubmenuAndRemoveFromFolderActOnTheSelection()
    {
        using var fixture = new ModuleExplorerFixture();
        fixture.Content.Add(ModuleResourceType.Nss, "one");
        var section = fixture.SeededSection(ModuleResourceType.Nss);
        var parent = section.AddFolder("Parent");
        var child = parent.AddChild("Child");
        var controller = fixture.Open(ModuleResourceType.Nss);
        controller.ToggleCommand.Execute(fixture.Row("Unsorted"));
        controller.SelectedRow = controller.Rows.Single(row => row.ResRef == "one");

        var target = controller.MoveTargets.Single(candidate => ReferenceEquals(candidate.Folder, child));
        Assert.AreEqual("Parent / Child", target.Path);
        target.Command.Execute(null);
        Assert.IsTrue(child.Members.Contains("one"));

        controller.ToggleCommand.Execute(fixture.FolderRow(parent));
        controller.ToggleCommand.Execute(fixture.FolderRow(child));
        controller.SelectedRow = controller.Rows.Single(row => row.ResRef == "one");
        controller.RemoveFromFolderCommand.Execute(null);

        Assert.IsFalse(section.FoldersContaining("one").Any());
    }

    [TestMethod]
    public void ResourcesFromAnotherTabOrNonFolderTargetsCannotBeDropped()
    {
        using var fixture = new ModuleExplorerFixture();
        fixture.Content.Add(ModuleResourceType.Nss, "one");
        fixture.Content.Add(ModuleResourceType.Nss, "two");
        fixture.SeededSection(ModuleResourceType.Nss);
        var controller = fixture.Open(ModuleResourceType.Nss);
        controller.ToggleCommand.Execute(fixture.Row("Unsorted"));
        var one = controller.Rows.Single(row => row.ResRef == "one");
        var two = controller.Rows.Single(row => row.ResRef == "two");

        Assert.IsFalse(controller.CanDropResource(one, two), "a resource is not a folder");
        Assert.IsFalse(controller.CanDropResource(one, fixture.Row("Unsorted")), "already unsorted");
    }
}
