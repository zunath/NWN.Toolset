using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Areas.Generation.Drafting;
using Nwn.Authoring.Areas.Generation.Hosting;
using Nwn.Authoring.Tests.Areas.Generation.Support;

namespace Nwn.Authoring.Tests.Areas.Generation.Drafting;

[TestClass]
public sealed class AreaGenerationAuthoringServiceTests
{
    private static AreaGenerationSettings Settings(string themeKey = GeneratorFixture.ThemeKey) => new()
    {
        ThemeKey = themeKey,
        Width = 20,
        Height = 20,
        Seed = 4242,
        Overrides = GeneratorFixture.CreateOverrides()
    };

    [TestMethod]
    public void ThemedDraftsAreDeterministicAndDressedFromTheHostTheme()
    {
        var service = new AreaGenerationAuthoringService(GeneratorFixture.CreateTilesetSource("hash"), GeneratorFixture.CreateCatalog(true));

        var first = service.Generate(Settings());
        var second = service.Generate(Settings());

        Assert.IsTrue(first.Result.Success, first.Result.FailureReason);
        Assert.AreEqual("hash", first.TilesetFingerprint);
        CollectionAssert.AreEqual(
            first.Result.Resolved!.Tiles.Select(tile => (tile.TileId, tile.Orientation, tile.Height)).ToArray(),
            second.Result.Resolved!.Tiles.Select(tile => (tile.TileId, tile.Orientation, tile.Height)).ToArray());
        CollectionAssert.AreEqual(
            first.Result.PlannedDecorations.Select(prop => (prop.Resref, prop.Position, prop.Facing)).ToArray(),
            second.Result.PlannedDecorations.Select(prop => (prop.Resref, prop.Position, prop.Facing)).ToArray());
        Assert.IsTrue(first.Result.PlannedDecorations.Count > 0);
        Assert.IsTrue(first.Result.PlannedDecorations.All(prop => prop.Resref == GeneratorFixture.PropResRef));
    }

    [TestMethod]
    public void ACatalogWithoutThemesComposesTheRequestedProfilesIntoGeometryOnly()
    {
        var service = new AreaGenerationAuthoringService(GeneratorFixture.CreateTilesetSource(), GeneratorFixture.CreateCatalog(false));

        var draft = service.Generate(Settings(string.Empty) with
        {
            TilesetProfileKey = GeneratorFixture.TilesetKey,
            LayoutProfileKey = GeneratorFixture.LayoutKey
        });

        Assert.IsTrue(draft.Result.Success, draft.Result.FailureReason);
        Assert.IsNull(draft.Composition.Content);
        Assert.AreEqual(0, draft.Result.PlannedDecorations.Count);
    }

    [TestMethod]
    public void InvalidRequestsAreRejectedBeforeSolving()
    {
        var service = new AreaGenerationAuthoringService(GeneratorFixture.CreateTilesetSource(), GeneratorFixture.CreateCatalog(true));

        Assert.ThrowsExactly<ArgumentException>(() => service.Generate(Settings("unknown")));
        Assert.ThrowsExactly<ArgumentException>(() => service.Generate(Settings() with { Tier = 9 }));
        Assert.ThrowsExactly<ArgumentException>(() => service.Generate(Settings() with { TilesetProfileKey = "missing" }));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => service.Generate(Settings() with { Width = 4 }));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => service.Generate(Settings() with { Seed = -1 }));
    }

    [TestMethod]
    public void ThemeSizeRangeBoundsTheRequestedDimensions()
    {
        var catalog = GeneratorFixture.CreateCatalog(true);
        catalog.Themes[0].MaxSize = 20;
        var service = new AreaGenerationAuthoringService(GeneratorFixture.CreateTilesetSource(), catalog);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => service.Generate(Settings() with { Width = 24 }));
    }

    [TestMethod]
    public void LayoutDiagnosticsReachTheHostLog()
    {
        var log = new CapturingLog();
        var service = new AreaGenerationAuthoringService(GeneratorFixture.CreateTilesetSource(), GeneratorFixture.CreateCatalog(true), log);

        service.Generate(Settings());

        Assert.IsTrue(log.Messages.All(message => message.StartsWith("Area layout diagnostic", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void MissingBlueprintsAreReportedTogetherBeforeAPreviewCanBeCreated()
    {
        var service = new AreaGenerationAuthoringService(GeneratorFixture.CreateTilesetSource(), GeneratorFixture.CreateCatalog(true));
        var blueprints = new FixtureBlueprintSource();

        var exception = Assert.ThrowsExactly<InvalidOperationException>(() => service.Generate(Settings(), blueprints));

        StringAssert.Contains(exception.Message, GeneratorFixture.ExitPlaceableResRef);
        StringAssert.Contains(exception.Message, GeneratorFixture.PropResRef);
    }

    [TestMethod]
    public void CompleteBlueprintsValidateEncounterPlacement()
    {
        var service = new AreaGenerationAuthoringService(GeneratorFixture.CreateTilesetSource(), GeneratorFixture.CreateCatalog(true));

        var draft = service.Generate(Settings(), FixtureBlueprintSource.CreateComplete());

        Assert.IsTrue(draft.Result.Success, draft.Result.FailureReason);
    }

    private sealed class CapturingLog : IAreaGenerationLog
    {
        public List<string> Messages { get; } = new();

        public void Information(string message) => Messages.Add(message);

        public void Warning(string message) => Messages.Add(message);

        public void Error(Exception exception, string message) => Messages.Add(message);
    }
}
