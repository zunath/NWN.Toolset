using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Resources;
using Nwn.Toolset.Avalonia.Tests.Support;

namespace Nwn.Toolset.Avalonia.Tests.Explorer;

[TestClass]
public sealed class ModuleExplorerProjectionTests
{
    [TestMethod]
    public void FoldersComePinnedFirstThenAlphabeticalWithUnsortedLast()
    {
        using var fixture = new ModuleExplorerFixture();
        fixture.Content.Add(ModuleResourceType.Nss, "alpha");
        fixture.Content.Add(ModuleResourceType.Nss, "beta");
        fixture.Content.Add(ModuleResourceType.Nss, "loose");
        var section = fixture.SeededSection(ModuleResourceType.Nss);
        section.AddFolder("Zeta").AddMember("alpha");
        section.AddFolder("Alpha").AddMember("beta");
        section.AddFolder("Pinned");
        section.Pin("Pinned");

        var controller = fixture.Open(ModuleResourceType.Nss);

        CollectionAssert.AreEqual(
            new[] { "Pinned", "Alpha", "Zeta", "Unsorted" },
            controller.Rows.Select(row => row.Name).ToArray());
        Assert.AreEqual(1, fixture.Row("Unsorted").Count);
        Assert.IsTrue(fixture.Row("Unsorted").IsUnsorted);
        Assert.IsNull(fixture.Row("Unsorted").Folder);
        Assert.AreEqual(1, fixture.Row("Zeta").Count);
    }

    [TestMethod]
    public void FolderCountsIncludeDescendantsAndIgnoreResourcesThatNoLongerExist()
    {
        using var fixture = new ModuleExplorerFixture();
        fixture.Content.Add(ModuleResourceType.Nss, "one");
        fixture.Content.Add(ModuleResourceType.Nss, "two");
        var section = fixture.SeededSection(ModuleResourceType.Nss);
        var parent = section.AddFolder("Parent");
        parent.AddMember("one");
        parent.AddMember("deleted_long_ago");
        parent.AddChild("Child").AddMember("two");

        fixture.Open(ModuleResourceType.Nss);

        Assert.AreEqual(2, fixture.Row("Parent").Count);
        Assert.AreEqual(0, fixture.Row("Unsorted").Count, "an empty Unsorted stays visible as the drag-out target");
    }

    [TestMethod]
    public void ExpandingAFolderPublishesItsChildrenAndTheExpansionSurvivesARefresh()
    {
        using var fixture = new ModuleExplorerFixture();
        fixture.Content.Add(ModuleResourceType.Nss, "one");
        var section = fixture.SeededSection(ModuleResourceType.Nss);
        section.AddFolder("Folder").AddMember("one");
        var controller = fixture.Open(ModuleResourceType.Nss);

        Assert.IsFalse(controller.Rows.Any(row => row.ResRef == "one"));
        controller.ToggleCommand.Execute(fixture.Row("Folder"));
        Assert.IsTrue(controller.Rows.Any(row => row.ResRef == "one"));

        controller.Refresh();

        Assert.IsTrue(fixture.Row("Folder").IsExpanded);
        Assert.IsTrue(controller.Rows.Any(row => row.ResRef == "one"));
    }

    [TestMethod]
    public void TabsCountEverySectionAndTheNewButtonNamesTheSelectedOne()
    {
        using var fixture = new ModuleExplorerFixture();
        fixture.Content.Add(ModuleResourceType.Area, "area_one");
        fixture.Content.Add(ModuleResourceType.Nss, "one");
        fixture.Content.Add(ModuleResourceType.Nss, "two");

        var controller = fixture.Open(ModuleResourceType.Nss);

        CollectionAssert.AreEqual(new[] { 1, 0, 2 }, controller.Tabs.Select(tab => tab.Count).ToArray());
        Assert.IsTrue(controller.Tabs.Single(tab => tab.Type == ModuleResourceType.Nss).IsSelected);
        Assert.AreEqual("New Script...", controller.NewItemLabel);
    }

    [TestMethod]
    public void ANeverOrganizedSectionIsSeededOnceAndRowsInsideFoldersDropTheFolderPrefix()
    {
        using var fixture = new ModuleExplorerFixture();
        fixture.Content.Add(ModuleResourceType.Area, "tat_one", "Tatooine - Anchorhead");
        fixture.Content.Add(ModuleResourceType.Area, "loose", "Loose");

        var controller = fixture.Open(ModuleResourceType.Area);
        controller.ToggleCommand.Execute(fixture.Row("Tatooine"));

        var section = fixture.Section(ModuleResourceType.Area);
        Assert.IsTrue(section.IsSeeded);
        Assert.AreEqual(1, fixture.Categories.Saves);
        Assert.AreEqual("Anchorhead", controller.Rows.Single(row => row.ResRef == "tat_one").Name);
        Assert.IsTrue(fixture.Log.Lines.Single().StartsWith("Organised areas into 1 folder(s).", StringComparison.Ordinal));

        section.RemoveFolder(section.Folders[0]);
        controller.Refresh();
        Assert.AreEqual(1, fixture.Organization.SeedCalls, "an emptied, seeded section is never re-seeded");
    }

    [TestMethod]
    public void SeedingWaitsUntilTheHostSaysItsNamesAreReady()
    {
        using var fixture = new ModuleExplorerFixture();
        fixture.Content.Add(ModuleResourceType.Area, "tat_one", "Tatooine - Anchorhead");
        fixture.Organization.Ready = false;

        fixture.Open(ModuleResourceType.Area);
        Assert.AreEqual(0, fixture.Organization.SeedCalls);

        fixture.Organization.Ready = true;
        fixture.Controller.Refresh();

        Assert.AreEqual(1, fixture.Organization.SeedCalls);
        Assert.IsTrue(fixture.Section(ModuleResourceType.Area).IsSeeded);
    }

    [TestMethod]
    public void SearchHidesFoldersWithoutMatchesAndRestoresThemWhenCleared()
    {
        using var fixture = new ModuleExplorerFixture();
        fixture.Content.Add(ModuleResourceType.Nss, "nanostation015");
        fixture.Content.Add(ModuleResourceType.Nss, "tatooine001");
        var section = fixture.SeededSection(ModuleResourceType.Nss);
        var stations = section.AddFolder("Stations");
        stations.AddChild("Nanostation").AddMember("nanostation015");
        stations.AddChild("Other Stations").AddMember("tatooine001");
        section.AddFolder("Empty Category");
        var controller = fixture.Open(ModuleResourceType.Nss);
        controller.ToggleCommand.Execute(fixture.Row("Stations"));

        controller.Filter = "nanostation015";

        CollectionAssert.AreEqual(new[] { "Stations", "Nanostation" }, controller.Rows.Select(row => row.Name).ToArray());

        controller.Filter = string.Empty;

        CollectionAssert.AreEqual(
            new[] { "Empty Category", "Stations", "Nanostation", "Other Stations", "Unsorted" },
            controller.Rows.Select(row => row.Name).ToArray());
    }

    [TestMethod]
    public void SelectingAResourceRowTellsTheHost()
    {
        using var fixture = new ModuleExplorerFixture();
        fixture.Content.Add(ModuleResourceType.Nss, "one", "One");
        fixture.SeededSection(ModuleResourceType.Nss);
        var controller = fixture.Open(ModuleResourceType.Nss);
        controller.ToggleCommand.Execute(fixture.Row("Unsorted"));

        controller.SelectedRow = controller.Rows.Single(row => row.ResRef == "one");

        Assert.AreEqual(ModuleResourceType.Nss, fixture.Selection.Selections.Single().Type);
        Assert.AreEqual("One", fixture.Selection.Selections.Single().Item.Name);
    }

    [TestMethod]
    public void TheSavedTabIsRestoredAndEveryTabChangeIsSaved()
    {
        using var fixture = new ModuleExplorerFixture();
        fixture.Settings.SelectedSection = ModuleResourceType.Dlg;

        var controller = fixture.Controller;
        Assert.AreEqual(ModuleResourceType.Dlg, controller.SelectedType);

        controller.SelectTabCommand.Execute(controller.Tabs.Single(tab => tab.Type == ModuleResourceType.Nss));

        Assert.AreEqual(ModuleResourceType.Nss, fixture.Settings.SelectedSection);
        Assert.IsTrue(controller.CanCompileSelectedType);
    }

    [TestMethod]
    public void ASavedTabTheHostNoLongerOffersFallsBackToTheFirstSection()
    {
        using var fixture = new ModuleExplorerFixture();
        fixture.Settings.SelectedSection = ModuleResourceType.Utc;

        Assert.AreEqual(ModuleResourceType.Area, fixture.Controller.SelectedType);
    }

    [TestMethod]
    public void ContentChangesRebuildTheTree()
    {
        using var fixture = new ModuleExplorerFixture();
        fixture.SeededSection(ModuleResourceType.Nss);
        var controller = fixture.Open(ModuleResourceType.Nss);

        fixture.Content.Add(ModuleResourceType.Nss, "late");
        fixture.Content.RaiseContentChanged();

        Assert.AreEqual(1, fixture.Row("Unsorted").Count);
        Assert.AreEqual(1, controller.Tabs.Single(tab => tab.Type == ModuleResourceType.Nss).Count);
    }
}
