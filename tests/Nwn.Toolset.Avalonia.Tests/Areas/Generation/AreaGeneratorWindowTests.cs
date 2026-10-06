using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Nwn.Toolset.Avalonia.Areas.Generation;
using Nwn.Toolset.Avalonia.Tests.Support;

namespace Nwn.Toolset.Avalonia.Tests.Areas.Generation;

[TestClass]
public sealed class AreaGeneratorWindowTests
{
    [TestMethod]
    public async Task WindowLoadsItsCompiledMarkupAndTitleFollowsTheHostOptions()
    {
        await GraphTestRuntime.DispatchAsync(() =>
        {
            var bare = new AreaGeneratorWindow();
            Assert.AreEqual("Area Generator", bare.Title);
            Assert.IsNotNull(bare.Content);
            bare.Close();

            using var viewModel = new AreaGeneratorViewModel(new GeneratorHostFixture().CreateHost());
            var window = new AreaGeneratorWindow(viewModel, new AreaGeneratorWindowOptions("Fixture Toolset"));
            Assert.AreEqual("Area Generator \u00b7 Fixture Toolset", window.Title);
            window.Close();
            return Task.CompletedTask;
        });
    }

    [TestMethod]
    public async Task ThemelessHostHidesThemeTierAndDecorationControlsButKeepsTheComposition()
    {
        await GraphTestRuntime.DispatchAsync(() =>
        {
            using var viewModel = new AreaGeneratorViewModel(new GeneratorHostFixture().CreateHost());
            var window = new AreaGeneratorWindow(viewModel);
            try
            {
                window.Show();
                Dispatcher.UIThread.RunJobs();

                Assert.IsFalse(window.FindControl<ComboBox>("TierSelector")!.IsVisible);
                Assert.IsTrue(window.FindControl<TextBox>("ResRefInput")!.IsVisible);
                Assert.IsFalse(window.FindControl<ComboBox>("DecorationPlacementSelector")!.IsEffectivelyVisible);
                Assert.IsTrue(window.FindControl<ComboBox>("PreviewModeSelector")!.IsVisible);
            }
            finally
            {
                window.Close();
                Dispatcher.UIThread.RunJobs();
            }

            return Task.CompletedTask;
        });
    }

    [TestMethod]
    public async Task OpeningTheWindowRandomizesTheSeedAndGeneratesAPreviewAutomatically()
    {
        await GraphTestRuntime.DispatchAsync(async () =>
        {
            using var viewModel = new AreaGeneratorViewModel(new GeneratorHostFixture().CreateHost());
            var seedBeforeOpening = viewModel.Seed;
            var window = new AreaGeneratorWindow(viewModel);
            try
            {
                window.Show();
                Dispatcher.UIThread.RunJobs();
                Assert.AreNotEqual(seedBeforeOpening, viewModel.Seed);

                var deadline = DateTime.UtcNow.AddSeconds(10);
                while (viewModel.Preview == null && DateTime.UtcNow < deadline)
                {
                    await Task.Delay(25);
                    Dispatcher.UIThread.RunJobs();
                }

                Assert.IsNotNull(viewModel.Preview, viewModel.StatusMessage);
            }
            finally
            {
                window.Close();
                Dispatcher.UIThread.RunJobs();
            }
        });
    }
}
