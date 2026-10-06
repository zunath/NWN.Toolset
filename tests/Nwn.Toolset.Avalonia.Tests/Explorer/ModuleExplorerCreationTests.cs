using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Resources;
using Nwn.Toolset.Avalonia.Explorer.Workflow;
using Nwn.Toolset.Avalonia.Tests.Support;

namespace Nwn.Toolset.Avalonia.Tests.Explorer;

[TestClass]
public sealed class ModuleExplorerCreationTests
{
    [TestMethod]
    public async Task ANamedScriptIsWrittenFiledIntoTheSelectedFolderAndOpened()
    {
        using var fixture = new ModuleExplorerFixture();
        var folder = fixture.SeededSection(ModuleResourceType.Nss).AddFolder("Utility");
        var controller = fixture.Open(ModuleResourceType.Nss);
        controller.SelectedRow = fixture.FolderRow(folder);
        fixture.Creation.Options = new("conversation");
        fixture.Prompts.Answers.Enqueue("My Script");

        await controller.NewItemCommand.ExecuteAsync(null);

        Assert.AreEqual("New Script", fixture.Prompts.Headlines.Single());
        Assert.AreEqual("Name for the new script. Its ResRef is derived from this.", fixture.Prompts.Messages.Single());
        var write = fixture.Creation.Writes.Single();
        Assert.AreEqual("my_script", write.ResRef);
        Assert.AreEqual("My Script", write.Name);
        Assert.AreEqual("conversation", write.Options.TemplateId);
        Assert.IsTrue(folder.Members.Contains("my_script"));
        Assert.AreEqual("Created script 'my_script'.", fixture.Log.Lines.Single());
        Assert.AreEqual(
            "Created 'my_script'. It must be compiled to .ncs by the build before the game will run it.",
            controller.StatusMessage);
        Assert.AreEqual((ModuleResourceType.Nss, "my_script"), fixture.Creation.CreatedNotices.Single());
        Assert.AreEqual((ModuleResourceType.Nss, "my_script"), fixture.Editors.Opened.Single());
    }

    [TestMethod]
    public async Task AnExistingResRefOrUnusableNameIsReportedWithoutWriting()
    {
        using var fixture = new ModuleExplorerFixture();
        fixture.Content.Add(ModuleResourceType.Dlg, "greeting");
        var controller = fixture.Open(ModuleResourceType.Dlg);

        fixture.Prompts.Answers.Enqueue("Greeting");
        await controller.NewItemCommand.ExecuteAsync(null);
        Assert.AreEqual("'greeting' already exists.", controller.StatusMessage);

        fixture.Prompts.Answers.Enqueue("!!!");
        await controller.NewItemCommand.ExecuteAsync(null);
        Assert.AreEqual("That name has no letters or digits to make a ResRef from.", controller.StatusMessage);

        Assert.AreEqual(0, fixture.Creation.Writes.Count);
    }

    [TestMethod]
    public async Task CancellingTheHostChoiceOrLockingDuringItWritesNothing()
    {
        using var fixture = new ModuleExplorerFixture();
        var controller = fixture.Open(ModuleResourceType.Nss);

        fixture.Creation.Options = null;
        fixture.Prompts.Answers.Enqueue("first");
        await controller.NewItemCommand.ExecuteAsync(null);
        Assert.AreEqual(0, fixture.Creation.Writes.Count);

        fixture.Creation.Options = ModuleExplorerCreationOptions.None;
        fixture.Creation.DuringOptions = () => fixture.WriteGate.Set(true);
        fixture.Prompts.Answers.Enqueue("second");
        await controller.NewItemCommand.ExecuteAsync(null);

        Assert.AreEqual(0, fixture.Creation.Writes.Count);
        Assert.AreEqual(
            "Creating resources is unavailable while another module operation is in progress.",
            controller.StatusMessage);
    }

    [TestMethod]
    public async Task AFailedWriteIsReported()
    {
        using var fixture = new ModuleExplorerFixture();
        var controller = fixture.Open(ModuleResourceType.Nss);
        fixture.Creation.CreateFailure = "disk full";
        fixture.Prompts.Answers.Enqueue("broken");

        await controller.NewItemCommand.ExecuteAsync(null);

        Assert.AreEqual("Could not create 'broken': disk full", controller.StatusMessage);
        Assert.AreEqual(0, fixture.Editors.Opened.Count);
    }

    [TestMethod]
    public async Task AResourceThatCannotBeFiledSaysItIsInUnsorted()
    {
        using var fixture = new ModuleExplorerFixture();
        var folder = fixture.SeededSection(ModuleResourceType.Dlg).AddFolder("Quests");
        var controller = fixture.Open(ModuleResourceType.Dlg);
        controller.SelectedRow = fixture.FolderRow(folder);
        fixture.Categories.Refusal = "Another program changed the sidecar.";
        fixture.Prompts.Answers.Enqueue("talk");

        await controller.NewItemCommand.ExecuteAsync(null);

        Assert.AreEqual(
            "Created 'talk', but it could not be filed under 'Quests' — it is in Unsorted. Another program changed the sidecar.",
            controller.StatusMessage);
    }

    [TestMethod]
    public async Task TheAreaFormFilesIntoTheFolderSelectedWhenItOpened()
    {
        using var fixture = new ModuleExplorerFixture();
        var areaFolder = fixture.SeededSection(ModuleResourceType.Area).AddFolder("Tatooine");
        var scriptFolder = fixture.SeededSection(ModuleResourceType.Nss).AddFolder("Utility");
        var controller = fixture.Open(ModuleResourceType.Area);
        controller.SelectedRow = fixture.FolderRow(areaFolder);

        await controller.NewItemCommand.ExecuteAsync(null);
        Assert.IsNotNull(controller.ActiveCreationForm);

        // The form is nonmodal: the builder switches tab and folder while it sits open.
        controller.SelectedType = ModuleResourceType.Nss;
        controller.SelectedRow = fixture.FolderRow(scriptFolder);
        fixture.Creation.CompleteForm("new_area");

        Assert.IsNull(controller.ActiveCreationForm);
        Assert.IsTrue(areaFolder.Members.Contains("new_area"));
        Assert.IsFalse(scriptFolder.Members.Contains("new_area"));
        Assert.AreEqual((ModuleResourceType.Area, "new_area"), fixture.Editors.Opened.Single());
    }

    [TestMethod]
    public async Task TheFormAsksTheWriteGateAtTheMomentItWrites()
    {
        using var fixture = new ModuleExplorerFixture();
        var controller = fixture.Open(ModuleResourceType.Area);
        await controller.NewItemCommand.ExecuteAsync(null);
        var callbacks = fixture.Creation.FormCallbacks!;

        Assert.IsTrue(callbacks.CanWrite());
        fixture.WriteGate.Set(true);
        Assert.IsFalse(callbacks.CanWrite());

        callbacks.Cancelled();
        Assert.IsNull(controller.ActiveCreationForm);
    }

    [TestMethod]
    public void CreatingIsUnavailableWhileTheModuleIsLocked()
    {
        using var fixture = new ModuleExplorerFixture();
        var controller = fixture.Open(ModuleResourceType.Area);
        Assert.IsTrue(controller.NewItemCommand.CanExecute(null));

        fixture.WriteGate.Set(true);

        Assert.IsFalse(controller.CanCreateSelectedType);
        Assert.IsFalse(controller.NewItemCommand.CanExecute(null));
        Assert.IsFalse(controller.CanCompileSelectedType);

        fixture.WriteGate.Set(false);
        Assert.IsTrue(controller.NewItemCommand.CanExecute(null));
    }
}
