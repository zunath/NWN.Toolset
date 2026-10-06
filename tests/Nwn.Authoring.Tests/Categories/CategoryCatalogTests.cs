using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Categories;
using Nwn.Authoring.Resources;

namespace Nwn.Authoring.Tests.Categories;

[TestClass]
public sealed class CategoryCatalogTests
{
    private string _root = string.Empty;

    private string SidecarPath => Path.Combine(_root, "toolset", "categories.json");

    [TestInitialize]
    public void Initialize()
    {
        _root = Path.Combine(Path.GetTempPath(), "nwn-category-catalog-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [TestMethod]
    public void DefaultPathKeepsSidecarOutsideModule()
    {
        var module = Path.Combine(_root, "Module");
        var sidecar = CategoryCatalog.DefaultPathFor(module);

        Assert.AreEqual(SidecarPath, sidecar);
        Assert.IsFalse(sidecar.Contains($"Module{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
    }

    [TestMethod]
    public void MissingFileLoadsAsWritableEmptyCatalog()
    {
        var catalog = CategoryCatalog.Load(SidecarPath, out var warning);

        Assert.IsNull(warning);
        Assert.IsFalse(catalog.IsReadOnly);
        Assert.IsFalse(catalog.IsDirty);
        CollectionAssert.AreEqual(Array.Empty<ModuleResourceType>(), catalog.Types.ToArray());
    }

    [TestMethod]
    public void NestedMembershipPinsAndGroupingRoundTrip()
    {
        var catalog = CategoryCatalog.Load(SidecarPath);
        var section = catalog.Section(ModuleResourceType.Utp);
        section.Grouping = CategoryGrouping.Folders;
        var interior = section.AddFolder("Interiors");
        var terminal = interior.AddChild("Consoles & Terminals");
        var repair = terminal.AddChild("Droid Repair");
        repair.AddMember("droid_01");
        interior.AddMember("cargo_01");
        section.Pin(section.PathKey(terminal));

        catalog.Save();

        var reloaded = CategoryCatalog.Load(SidecarPath);
        var loadedSection = reloaded.Section(ModuleResourceType.Utp);
        Assert.AreEqual(CategoryGrouping.Folders, loadedSection.Grouping);
        CollectionAssert.AreEqual(new[] { "Interiors/Consoles & Terminals" }, loadedSection.Pinned.ToArray());
        CollectionAssert.AreEqual(new[] { "droid_01" }, loadedSection.Find("Interiors", "Consoles & Terminals", "Droid Repair")!.Members.ToArray());
        CollectionAssert.AreEqual(new[] { "cargo_01" }, loadedSection.Find("Interiors")!.Members.ToArray());
        Assert.IsFalse(reloaded.IsDirty);
    }

    [TestMethod]
    public void CorruptSidecarRemainsReadOnlyAndUnchanged()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SidecarPath)!);
        const string corrupt = "{ this is not json";
        File.WriteAllText(SidecarPath, corrupt);

        var catalog = CategoryCatalog.Load(SidecarPath, out var warning);

        Assert.IsTrue(catalog.IsReadOnly);
        Assert.IsNotNull(warning);
        StringAssert.Contains(catalog.ReadOnlyReason!, "Could not read categories");
        Assert.ThrowsExactly<InvalidOperationException>(() => catalog.Save());
        Assert.AreEqual(corrupt, File.ReadAllText(SidecarPath));
    }

    [TestMethod]
    public void NewerSidecarRemainsReadableButCannotBeRewritten()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SidecarPath)!);
        const string json = "{ \"version\": 999, \"sections\": { \"utc\": { \"folders\": [ { \"name\": \"Troopers\" } ] } } }";
        File.WriteAllText(SidecarPath, json);

        var catalog = CategoryCatalog.Load(SidecarPath, out var warning);

        Assert.IsTrue(catalog.IsReadOnly);
        Assert.IsNotNull(warning);
        Assert.AreEqual("Troopers", catalog.Section(ModuleResourceType.Utc).Folders.Single().Name);
        Assert.ThrowsExactly<InvalidOperationException>(() => catalog.Save());
        Assert.AreEqual(json, File.ReadAllText(SidecarPath));
    }

    [TestMethod]
    public void SaveUsesEstablishedExtensionKeysAndLeavesNoTemporaryFile()
    {
        var catalog = CategoryCatalog.Load(SidecarPath);
        catalog.Section(ModuleResourceType.Area).AddFolder("Named areas");
        catalog.Section(ModuleResourceType.Nss).AddFolder("Scripts");

        catalog.Save();

        using var json = JsonDocument.Parse(File.ReadAllBytes(SidecarPath));
        var sections = json.RootElement.GetProperty("sections");
        Assert.IsTrue(sections.TryGetProperty("are", out _));
        Assert.IsTrue(sections.TryGetProperty("nss", out _));
        Assert.IsFalse(sections.TryGetProperty("area", out _));
        Assert.IsFalse(File.Exists(SidecarPath + ".tmp"));
    }
}
