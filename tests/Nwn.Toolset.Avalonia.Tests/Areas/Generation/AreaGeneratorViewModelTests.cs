using Nwn.Authoring.Areas.Generation;
using Nwn.Authoring.Areas.Generation.Preview;
using Nwn.Toolset.Avalonia.Areas.Generation;
using Nwn.Toolset.Avalonia.Localization;
using Nwn.Toolset.Avalonia.Tests.Support;

namespace Nwn.Toolset.Avalonia.Tests.Areas.Generation;

[TestClass]
public sealed class AreaGeneratorViewModelTests
{
    private static AreaGeneratorViewModel Create(GeneratorHostFixture fixture) => new(fixture.CreateHost());

    [TestMethod]
    public void ChoicesComeFromTheHostCatalogAndAThemelessHostHidesThemePickers()
    {
        using var viewModel = Create(new GeneratorHostFixture());

        Assert.IsFalse(viewModel.HasThemes);
        Assert.AreEqual(0, viewModel.Themes.Count);
        CollectionAssert.AreEqual(new[] { "Fixture tileset" }, viewModel.TilesetProfiles.Select(choice => choice.Label).ToArray());
        CollectionAssert.AreEqual(new[] { "Fixture rooms" }, viewModel.LayoutProfiles.Select(choice => choice.Label).ToArray());
        Assert.AreEqual("Fixture tileset", viewModel.SelectedTilesetProfile!.Label);
        Assert.IsTrue(viewModel.GeneratePreviewCommand.CanExecute(null));
        Assert.AreEqual("Generated Area", viewModel.DisplayName);
        Assert.AreEqual(AreaGeneratorTexts.English.Get(AreaGeneratorStringId.StatusCompositionChanged), viewModel.StatusMessage);
    }

    [TestMethod]
    public async Task PreviewUsesTheHostBackgroundRunnerAndOverlayChangesReuseTheSolvedDraft()
    {
        await GraphTestRuntime.DispatchAsync(async () =>
        {
            var fixture = new GeneratorHostFixture();
            using var viewModel = Create(fixture);
            viewModel.PreviewMode = AreaPreviewMode.Schematic;
            viewModel.Width = 16;
            viewModel.Height = 16;

            await viewModel.GeneratePreviewCommand.ExecuteAsync(null);
            Assert.IsNotNull(viewModel.Preview, viewModel.StatusMessage);
            Assert.AreEqual(1, fixture.Tasks.OperationTypes.Count(type => type.Name == "AreaGenerationDraft"));

            viewModel.ShowRoutes = true;
            await viewModel.GeneratePreviewCommand.ExecuteAsync(null);
            Assert.IsNotNull(viewModel.Preview);
            Assert.AreEqual(1, fixture.Tasks.OperationTypes.Count(type => type.Name == "AreaGenerationDraft"));
        });
    }

    [TestMethod]
    public async Task CreatingAPreviewedAreaHandsTheCanonicalRequestToTheHostWriter()
    {
        await GraphTestRuntime.DispatchAsync(async () =>
        {
            var fixture = new GeneratorHostFixture();
            using var viewModel = Create(fixture);
            string? created = null;
            viewModel.AreaCreated += resRef => created = resRef;
            viewModel.PreviewMode = AreaPreviewMode.Schematic;
            viewModel.ResRef = " Fixture_Area ";
            viewModel.DisplayName = "Fixture area";

            await viewModel.GeneratePreviewCommand.ExecuteAsync(null);
            Assert.IsTrue(viewModel.CreateAreaCommand.CanExecute(null), viewModel.StatusMessage);
            await viewModel.CreateAreaCommand.ExecuteAsync(null);

            Assert.AreEqual("fixture_area", created);
            var request = fixture.Writer.Requests.Single();
            Assert.AreEqual("fixture_area", request.ResRef);
            Assert.AreEqual("Fixture area", request.DisplayName);
            Assert.AreEqual("fixture-hash", request.Draft.TilesetFingerprint);
            Assert.IsNotNull(request.Populate);
            StringAssert.Contains(viewModel.StatusMessage, "fixture_area");
        });
    }

    [TestMethod]
    public async Task InvalidResRefsAndHostRefusalsAreReportedWithoutClosing()
    {
        await GraphTestRuntime.DispatchAsync(async () =>
        {
            var fixture = new GeneratorHostFixture { Writer = { FailureReason = "The module is read-only." } };
            using var viewModel = Create(fixture);
            string? created = null;
            viewModel.AreaCreated += resRef => created = resRef;
            viewModel.PreviewMode = AreaPreviewMode.Schematic;
            await viewModel.GeneratePreviewCommand.ExecuteAsync(null);

            viewModel.ResRef = "Not Valid!";
            await viewModel.CreateAreaCommand.ExecuteAsync(null);
            Assert.IsTrue(viewModel.HasResRefError);
            Assert.AreEqual(0, fixture.Writer.Requests.Count);

            viewModel.ResRef = "valid_area";
            await viewModel.CreateAreaCommand.ExecuteAsync(null);
            Assert.IsFalse(viewModel.HasResRefError);
            Assert.IsTrue(viewModel.StatusIsError);
            Assert.AreEqual("The module is read-only.", viewModel.StatusMessage);
            Assert.IsNull(created);
        });
    }

    [TestMethod]
    public async Task ChangingASettingInvalidatesThePreviewSoAStaleDraftCannotBeCreated()
    {
        await GraphTestRuntime.DispatchAsync(async () =>
        {
            var fixture = new GeneratorHostFixture();
            using var viewModel = Create(fixture);
            viewModel.PreviewMode = AreaPreviewMode.Schematic;
            viewModel.ResRef = "stale_area";
            await viewModel.GeneratePreviewCommand.ExecuteAsync(null);
            Assert.IsTrue(viewModel.CreateAreaCommand.CanExecute(null));

            viewModel.Seed += 1;

            Assert.IsNull(viewModel.Preview);
            Assert.IsFalse(viewModel.CreateAreaCommand.CanExecute(null));
        });
    }

    [TestMethod]
    public void HostTextsReplaceEnglishEverywhere()
    {
        var texts = AreaGeneratorTexts.English;
        var values = Enum.GetValues<AreaGeneratorStringId>().ToDictionary(id => id, id => "x" + id);
        var host = new GeneratorHostFixture().CreateHost() with { Texts = new AreaGeneratorTexts(values) };

        using var viewModel = new AreaGeneratorViewModel(host);

        Assert.AreEqual("x" + AreaGeneratorStringId.StatusCompositionChanged, viewModel.StatusMessage);
        Assert.AreEqual("x" + AreaGeneratorStringId.DefaultDisplayName, viewModel.DisplayName);
        Assert.AreEqual("x" + AreaGeneratorStringId.PageTitle, viewModel.Labels.PageTitle);
        Assert.AreNotEqual(texts.Get(AreaGeneratorStringId.PageTitle), viewModel.Labels.PageTitle);
        Assert.ThrowsExactly<ArgumentException>(() => new AreaGeneratorTexts(new Dictionary<AreaGeneratorStringId, string>()));
    }

    [TestMethod]
    public void EnumChoicesAreLabelledFromTheHostTexts()
    {
        var converter = new AreaGeneratorOptionLabelConverter(AreaGeneratorTexts.English);

        Assert.AreEqual("Map graphics", converter.Convert(AreaPreviewMode.MapGraphics, typeof(string), null, System.Globalization.CultureInfo.InvariantCulture));
        Assert.AreEqual("Organic cave", converter.Convert(DungeonLayoutStyle.OrganicCave, typeof(string), null, System.Globalization.CultureInfo.InvariantCulture));
        Assert.AreEqual("Compact dressing", converter.Convert(Nwn.Authoring.Areas.Generation.Decoration.DecorationPlacementStyle.Compact, typeof(string), null, System.Globalization.CultureInfo.InvariantCulture));
    }
}
