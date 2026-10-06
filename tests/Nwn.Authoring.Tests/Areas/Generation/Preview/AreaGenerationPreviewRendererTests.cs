using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Areas.Generation.Drafting;
using Nwn.Authoring.Areas.Generation.Hosting;
using Nwn.Authoring.Areas.Generation.Preview;
using Nwn.Authoring.Tests.Areas.Generation.Support;

namespace Nwn.Authoring.Tests.Areas.Generation.Preview;

[TestClass]
public sealed class AreaGenerationPreviewRendererTests
{
    private static AreaGenerationDraft Generate()
    {
        var service = new AreaGenerationAuthoringService(GeneratorFixture.CreateTilesetSource(), GeneratorFixture.CreateCatalog(true));
        return service.Generate(new AreaGenerationSettings
        {
            ThemeKey = GeneratorFixture.ThemeKey, Width = 20, Height = 20, Seed = 4242, Overrides = GeneratorFixture.CreateOverrides()
        });
    }

    [TestMethod]
    public void SchematicRenderingIsSizedByTilesAndDeterministic()
    {
        var draft = Generate();
        var renderer = new AreaGenerationPreviewRenderer(null);

        var first = renderer.Render(draft, AreaPreviewMode.Schematic, true, pixelsPerTile: 8);
        var second = renderer.Render(draft, AreaPreviewMode.Schematic, true, pixelsPerTile: 8);

        Assert.AreEqual(20 * 8, first.Width);
        Assert.AreEqual(20 * 8, first.Height);
        Assert.AreEqual(first.Width * first.Height * 4, first.Pixels.Length);
        CollectionAssert.AreEqual(first.Pixels, second.Pixels);
        Assert.AreEqual(0, first.MissingTileGraphics);
    }

    [TestMethod]
    public void MapModeAsksTheHostForTileGraphicsAndFallsBackWhenTheyAreMissing()
    {
        var draft = Generate();
        var graphics = new CountingGraphics(supplyImages: false);

        var image = new AreaGenerationPreviewRenderer(graphics).Render(draft, AreaPreviewMode.MapGraphics, false, pixelsPerTile: 8);

        Assert.AreEqual(0, graphics.Requests, "The fixture tiles name no 2D map picture.");
        Assert.AreEqual(draft.Result.Resolved!.Tiles.Length, image.MissingTileGraphics);
    }

    [TestMethod]
    public void OverlaysChangeThePixelsAndInvalidSizesAreRejected()
    {
        var draft = Generate();
        var renderer = new AreaGenerationPreviewRenderer(null);

        var plain = renderer.Render(draft, AreaPreviewMode.Schematic, false, showTransitions: false, showDecorations: false, pixelsPerTile: 8);
        var withRooms = renderer.Render(draft, AreaPreviewMode.Schematic, true, showTransitions: false, showDecorations: false, pixelsPerTile: 8);

        CollectionAssert.AreNotEqual(plain.Pixels, withRooms.Pixels);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => renderer.Render(draft, AreaPreviewMode.Schematic, false, pixelsPerTile: 4));
    }

    private sealed class CountingGraphics : IAreaPreviewTileGraphics
    {
        private readonly bool _supplyImages;

        public CountingGraphics(bool supplyImages) => _supplyImages = supplyImages;

        public int Requests { get; private set; }

        public bool TryLoadTileImage(string imageMap2D, out AreaPreviewTexture texture)
        {
            Requests++;
            texture = _supplyImages ? new AreaPreviewTexture(1, 1, [255, 0, 0, 255]) : null!;
            return _supplyImages;
        }
    }
}
