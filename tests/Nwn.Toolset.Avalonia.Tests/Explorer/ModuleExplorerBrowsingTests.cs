using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Resources;
using Nwn.Toolset.Avalonia.Explorer.Workflow;
using Nwn.Toolset.Avalonia.Tests.Support;

namespace Nwn.Toolset.Avalonia.Tests.Explorer;

[TestClass]
public sealed class ModuleExplorerBrowsingTests
{
    [TestMethod]
    public void OpeningAResourceAsksTheHostAndOpeningAFolderExpandsIt()
    {
        using var fixture = new ModuleExplorerFixture();
        fixture.Content.Add(ModuleResourceType.Dlg, "greeting");
        fixture.SeededSection(ModuleResourceType.Dlg);
        var controller = fixture.Open(ModuleResourceType.Dlg);

        controller.SelectedRow = fixture.Row("Unsorted");
        controller.OpenSelectedItem();
        Assert.IsTrue(fixture.Row("Unsorted").IsExpanded);

        controller.SelectedRow = controller.Rows.Single(row => row.ResRef == "greeting");
        controller.OpenSelectedCommand.Execute(null);

        Assert.AreEqual((ModuleResourceType.Dlg, "greeting"), fixture.Editors.Opened.Single());
    }

    [TestMethod]
    public async Task CompileIsOfferedOnlyOnTheCompilableTabAndClearsItsStatus()
    {
        using var fixture = new ModuleExplorerFixture();
        fixture.Content.Add(ModuleResourceType.Nss, "script");
        fixture.SeededSection(ModuleResourceType.Nss);
        var controller = fixture.Open(ModuleResourceType.Area);
        Assert.IsFalse(controller.CanCompileSelectedType);

        controller.SelectedType = ModuleResourceType.Nss;
        Assert.IsTrue(controller.CompileSelectedCommand.CanExecute(null));
        controller.ToggleCommand.Execute(fixture.Row("Unsorted"));
        controller.SelectedRow = controller.Rows.Single(row => row.ResRef == "script");

        await controller.CompileSelectedCommand.ExecuteAsync(null);

        Assert.AreEqual((ModuleResourceType.Nss, "script"), fixture.Editors.Compiled.Single());
        Assert.IsNull(controller.StatusMessage);
    }

    [TestMethod]
    public void WithoutOptionalServicesThePanelStillBrowses()
    {
        var content = new FakeExplorerContentSource();
        content.Add(ModuleResourceType.Nss, "one");
        var categories = new FakePaletteCategoryStore();
        using var controller = new ModuleExplorerController(
            new ModuleExplorerHost(content, categories));
        controller.SelectedType = ModuleResourceType.Nss;
        controller.Initialize();

        Assert.IsFalse(controller.CanCreateSelectedType);
        Assert.IsFalse(controller.CanOpenSelectedType);
        Assert.AreEqual(1, controller.Rows.Single(row => row.IsUnsorted).Count);
        Assert.AreEqual(0, categories.Live.Section(ModuleResourceType.Nss).Folders.Count, "no organization, no seeding");
    }
}
