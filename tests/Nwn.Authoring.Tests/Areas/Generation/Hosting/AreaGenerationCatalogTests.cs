using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Areas.Generation.Composition;
using Nwn.Authoring.Areas.Generation.Hosting;
using Nwn.Authoring.Tests.Areas.Generation.Support;

namespace Nwn.Authoring.Tests.Areas.Generation.Hosting;

[TestClass]
public sealed class AreaGenerationCatalogTests
{
    [TestMethod]
    public void ThemesListByDisplayNameAndLaterDefinitionsReplaceEarlierOnesByKey()
    {
        var catalog = new AreaGenerationCatalog(
            [
                new DungeonDetail { ThemeKey = "b", DisplayName = "Zeta" },
                new DungeonDetail { ThemeKey = "a", DisplayName = "Alpha" },
                new DungeonDetail { ThemeKey = "b", DisplayName = "Beta" }
            ],
            [GeneratorFixture.CreateTilesetProfile()],
            [GeneratorFixture.CreateLayoutProfile()]);

        CollectionAssert.AreEqual(new[] { "Alpha", "Beta" }, catalog.Themes.Select(theme => theme.DisplayName).ToArray());
        Assert.IsTrue(catalog.TilesetProfiles.ContainsKey(GeneratorFixture.TilesetKey));
        Assert.IsTrue(catalog.LayoutProfiles.ContainsKey(GeneratorFixture.LayoutKey));
    }

    [TestMethod]
    public void PaletteVariantsInheritTheirFamilyDressingOnce()
    {
        var family = GeneratorFixture.CreateTilesetProfile();
        family.Decorations.Add(new Nwn.Authoring.Areas.Generation.Decoration.DungeonDecorationEntry { Resref = GeneratorFixture.PropResRef });
        var variant = GeneratorFixture.CreateTilesetProfile();
        variant.Key = "variant";
        variant.IsPaletteVariant = true;

        var catalog = new AreaGenerationCatalog([], [family, variant], []);

        Assert.AreEqual(1, catalog.TilesetProfiles["variant"].Decorations.Count);
    }

    [TestMethod]
    public void EmptyCatalogIsValidAndOffersNoThemes()
    {
        var catalog = new AreaGenerationCatalog([], [], []);

        Assert.AreEqual(0, catalog.Themes.Count);
        Assert.AreEqual(0, catalog.TilesetProfiles.Count);
    }
}
