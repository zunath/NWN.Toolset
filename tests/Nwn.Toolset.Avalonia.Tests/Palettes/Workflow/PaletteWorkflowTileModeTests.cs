using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Areas.Tiles;
using Nwn.Formats.Tilesets;
using Nwn.Toolset.Avalonia.Palettes;
using Nwn.Toolset.Avalonia.Palettes.Workflow;
using Nwn.Toolset.Avalonia.Tests.Support;

namespace Nwn.Toolset.Avalonia.Tests.Palettes.Workflow;

[TestClass]
public sealed class PaletteWorkflowTileModeTests
{
    [TestMethod]
    public void TilesWithNoAreaShowTheEmptyStateInsteadOfAStatusLine()
    {
        using var palette = new PaletteWorkflowFixture();

        palette.Controller.SelectMode(PaletteMode.Tiles);

        Assert.IsTrue(palette.Controller.NeedsOpenArea);
        Assert.IsTrue(palette.State.NeedsOpenArea);
        Assert.AreEqual(string.Empty, palette.Controller.StatusMessage);
        Assert.AreEqual("Tileset content - read-only", palette.Controller.ReadOnlyNotice);
        Assert.IsFalse(palette.Controller.CanWrite);

        palette.Controller.SelectMode(PaletteMode.Blueprints);
        Assert.IsFalse(palette.Controller.NeedsOpenArea);
    }

    [TestMethod]
    public void OpeningAnAreaTakesTheStateDownAndReportsItsTileset()
    {
        using var palette = new PaletteWorkflowFixture();
        palette.Controller.SelectMode(PaletteMode.Tiles);

        palette.Placement.ActiveTarget = new FakePalettePlacementTarget { TilesetResRef = "tin01" };
        palette.Controller.OnActiveAreaChanged();

        Assert.IsFalse(palette.Controller.NeedsOpenArea);
        Assert.AreEqual("Tileset 'tin01' could not be loaded.", palette.Controller.StatusMessage);

        palette.Tilesets.Tilesets["tin01"] = new PaletteTileset(TilePalette.Empty, "Interior");
        palette.Controller.OnActiveAreaChanged();
        Assert.AreEqual("Tileset 'tin01' lists no tiles.", palette.Controller.StatusMessage);
    }

    [TestMethod]
    public void LeavingTilesClearsTheTilesetStatus()
    {
        using var palette = new PaletteWorkflowFixture();
        palette.Controller.SelectMode(PaletteMode.Tiles);
        palette.Placement.ActiveTarget = new FakePalettePlacementTarget { TilesetResRef = "tin01" };
        palette.Controller.OnActiveAreaChanged();
        Assert.AreEqual("Tileset 'tin01' could not be loaded.", palette.Controller.StatusMessage);

        palette.Controller.SelectMode(PaletteMode.Blueprints);

        Assert.AreEqual(string.Empty, palette.Controller.StatusMessage);
    }

    [TestMethod]
    public void PaintModeIsRememberedAndOnlyTilesShowTheSwitch()
    {
        using var palette = new PaletteWorkflowFixture();

        palette.Controller.SelectTilePaintMode(PaletteTilePaintMode.Manual);

        Assert.AreEqual(TilePaintMode.Manual, palette.Settings.TilePaintMode);
        Assert.IsFalse(palette.Controller.ShowsTilePaintSwitch);
        palette.Controller.SelectMode(PaletteMode.Tiles);
        Assert.IsTrue(palette.Controller.ShowsTilePaintSwitch);
        Assert.IsFalse(palette.Controller.ShowsSourceSwitch);
    }

    [TestMethod]
    public void SwitchingPaintModeKeepsACategoryBothModesOffer()
    {
        using var palette = new PaletteWorkflowFixture();
        palette.Tilesets.Tilesets["tin01"] = new PaletteTileset(TilePaletteBuilder.Build(TerrainAndFeatureTileset()), "Interior");
        palette.Placement.ActiveTarget = new FakePalettePlacementTarget { TilesetResRef = "tin01" };
        palette.Controller.SelectMode(PaletteMode.Tiles);
        CollectionAssert.AreEqual(
            new[]
            {
                TilePaletteBuilder.TerrainCategoryName,
                TilePaletteBuilder.FeaturesCategoryName,
                TilePaletteBuilder.GroupsCategoryName
            },
            palette.State.Rows.Select(row => row.Name).ToArray());
        palette.SelectRow(TilePaletteBuilder.GroupsCategoryName);

        palette.Controller.SelectTilePaintMode(PaletteTilePaintMode.Manual);

        CollectionAssert.AreEqual(
            new[]
            {
                TilePaletteBuilder.FeaturesCategoryName,
                TilePaletteBuilder.GroupsCategoryName,
                TilePaletteBuilder.AllTilesCategoryName
            },
            palette.State.Rows.Select(row => row.Name).ToArray());
        Assert.AreEqual(TilePaletteBuilder.GroupsCategoryName, palette.State.SelectedRow?.Name);
    }

    /// <summary>
    /// One solid terrain tile (Terrain and All tiles), a one-row group (Features) and a two-row group
    /// (Groups): Groups is third in Auto and second in Manual.
    /// </summary>
    private static TilesetDefinition TerrainAndFeatureTileset() => new()
    {
        Name = "tin01",
        Terrains = new[] { new TerrainDefinition("Ground", null, null) },
        Tiles = new[]
        {
            new TileDefinition
            {
                Model = "tin01_a01",
                TopLeft = "Ground",
                TopRight = "Ground",
                BottomLeft = "Ground",
                BottomRight = "Ground"
            },
            new TileDefinition { Model = "tin01_f01" }
        },
        Groups = new[]
        {
            new TileGroupDefinition("Pillar", 1, 1, null, new[] { 1 }),
            new TileGroupDefinition("Tower", 2, 1, null, new[] { 1, 1 })
        }
    };
}
