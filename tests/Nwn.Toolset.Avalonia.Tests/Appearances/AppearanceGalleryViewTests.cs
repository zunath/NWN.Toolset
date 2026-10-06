using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Toolset.Avalonia.Appearances;
using Nwn.Toolset.Avalonia.Appearances.Views;
using Nwn.Toolset.Avalonia.Tests.Support;

namespace Nwn.Toolset.Avalonia.Tests.Appearances;

[TestClass]
public sealed class AppearanceGalleryViewTests
{
    [TestMethod]
    public async Task MountedGalleryKeepsTheSourceLayoutAndPublishesOnePageAsync()
    {
        using var state = CreateState(Enumerable.Range(0, 200).Select(Option).ToArray(), new PreviewProviderSpy());
        Assert.AreEqual(48, state.Tiles.Count);
        Assert.IsTrue(state.CanLoadMore);
        Assert.AreEqual("48 of 200 models", state.MatchSummary);

        await GraphTestRuntime.RunAsync(() => new AppearanceGalleryView { DataContext = state }, window =>
        {
            var view = (AppearanceGalleryView)window.Content!;
            view.UpdateLayout();
            Assert.IsTrue(view.GetVisualDescendants().OfType<TextBox>().Any(), "The original search row is present.");
            Assert.IsTrue(view.GetVisualDescendants().OfType<TextBlock>().Any(block => block.Text == state.MatchSummary));
            var grid = view.GetVisualDescendants().OfType<ListBox>()
                .Single(list => ReferenceEquals(list.ItemsSource, state.Tiles));
            Assert.IsNotNull(grid.ItemsPanel);
            Assert.IsTrue(state.Tiles.All(tile => tile.TileSize == 112));
            state.LoadMoreCommand.Execute(null);
            Assert.AreEqual(96, state.Tiles.Count);
        });
    }

    [TestMethod]
    public async Task ReplacingAndDisposingGalleryClearsTileReferencesWithoutDisposingHostBitmapsAsync()
    {
        var provider = new PreviewProviderSpy();
        using var state = CreateState(new[] { Option(0) }, provider);
        await GraphTestRuntime.RunAsync(() => new AppearanceGalleryView { DataContext = state }, _ =>
        {
            using var originalBitmap = new global::Avalonia.Media.Imaging.WriteableBitmap(
                new global::Avalonia.PixelSize(1, 1),
                new global::Avalonia.Vector(96, 96),
                global::Avalonia.Platform.PixelFormat.Bgra8888,
                global::Avalonia.Platform.AlphaFormat.Unpremul);
            var originalTile = state.Tiles[0];
            provider.Ready!(originalBitmap);
            Dispatcher.UIThread.RunJobs();
            Assert.AreSame(originalBitmap, originalTile.Preview);

            state.SetOptions(new[] { Option(1) });
            Assert.IsNull(originalTile.Preview, "A replaced page must release references held by retained virtualized rows.");
            Assert.AreEqual(1, originalBitmap.PixelSize.Width, "The host preview provider owns bitmap disposal.");

            state.SetOptions(new[] { Option(0) });
            var activeTile = state.Tiles[0];
            using var activeBitmap = new global::Avalonia.Media.Imaging.WriteableBitmap(
                new global::Avalonia.PixelSize(1, 1),
                new global::Avalonia.Vector(96, 96),
                global::Avalonia.Platform.PixelFormat.Bgra8888,
                global::Avalonia.Platform.AlphaFormat.Unpremul);
            provider.Ready!(activeBitmap);
            Dispatcher.UIThread.RunJobs();
            Assert.AreSame(activeBitmap, activeTile.Preview);

            state.Dispose();
            Assert.IsNull(activeTile.Preview);
            Assert.AreEqual(1, activeBitmap.PixelSize.Width, "Disposing gallery state clears references without disposing host-owned bitmaps.");
        });
    }

    [TestMethod]
    public async Task ReplacingOptionsRejectsStalePicksAndPreviewCallbacksAsync()
    {
        var provider = new PreviewProviderSpy();
        using var state = CreateState(new[] { Option(0) }, provider);
        var oldTile = state.Tiles[0];
        Assert.IsNotNull(provider.Ready);

        await GraphTestRuntime.RunAsync(() => new AppearanceGalleryView { DataContext = state }, _ =>
        {
            state.SetOptions(new[] { Option(1) });
            state.Highlighted = oldTile;
            using var bitmap = new global::Avalonia.Media.Imaging.WriteableBitmap(
                new global::Avalonia.PixelSize(1, 1),
                new global::Avalonia.Vector(96, 96),
                global::Avalonia.Platform.PixelFormat.Bgra8888,
                global::Avalonia.Platform.AlphaFormat.Unpremul);
            provider.Ready!(bitmap);
            Dispatcher.UIThread.RunJobs();

            Assert.AreEqual(0, provider.Picks);
            Assert.IsNull(oldTile.Preview);
            Assert.AreEqual("1", state.Tiles[0].Option.Id.Value);
        });
    }

    [TestMethod]
    public void CurrentDescriptionUsesHostSuppliedCatalogFormatting()
    {
        var texts = new AppearanceGalleryTexts(new Dictionary<AppearanceGalleryStringId, string>
        {
            [AppearanceGalleryStringId.DefaultNoun] = "choice",
            [AppearanceGalleryStringId.NoMatches] = "none {0}",
            [AppearanceGalleryStringId.OneMatch] = "one {0} {1}",
            [AppearanceGalleryStringId.ManyMatches] = "many {0} {1}",
            [AppearanceGalleryStringId.PartialMatches] = "some {0}/{1} {2}",
            [AppearanceGalleryStringId.SearchWatermark] = "find {0}",
            [AppearanceGalleryStringId.UnknownCurrent] = "unknown {0}",
            [AppearanceGalleryStringId.CurrentWithDetail] = "{0} :: {1}"
        });
        using var state = new AppearanceGalleryViewModel(
            new[] { Option(0) },
            previews: null,
            currentId: () => new AppearanceGalleryOptionId("0"),
            apply: _ => true,
            texts: texts);

        Assert.AreEqual("Option 0 :: model_0", state.CurrentDescription);
    }

    [TestMethod]
    public void SnapshotCopiesOptionsAndRejectsDuplicateOrDefaultIdentities()
    {
        var options = new List<AppearanceGalleryOption> { Option(0) };
        using var state = CreateState(options, null);
        options.Add(Option(1));
        Assert.AreEqual(1, state.Tiles.Count);
        Assert.Throws<ArgumentException>(() => state.SetOptions(new[] { Option(1), Option(1) }));
        Assert.Throws<ArgumentException>(() => state.SetOptions(new[]
        {
            new AppearanceGalleryOption(default, "Invalid")
        }));
    }

    private static AppearanceGalleryViewModel CreateState(
        IReadOnlyList<AppearanceGalleryOption> options,
        PreviewProviderSpy? provider) =>
        new(options, provider, () => new AppearanceGalleryOptionId("0"), _ =>
        {
            if (provider != null)
                provider.Picks++;
            return true;
        });

    private static AppearanceGalleryOption Option(int index) =>
        new(new AppearanceGalleryOptionId(index.ToString()), $"Option {index}", $"model_{index}");

    private sealed class PreviewProviderSpy : IAppearanceGalleryPreviewProvider
    {
        public Action<global::Avalonia.Media.Imaging.Bitmap>? Ready { get; private set; }
        public int Picks { get; set; }

        public global::Avalonia.Media.Imaging.Bitmap? Cached(AppearanceGalleryOption option) => null;

        public bool Request(
            AppearanceGalleryOption option,
            Action<global::Avalonia.Media.Imaging.Bitmap> onReady,
            Action onFailed,
            AppearanceGalleryPreviewPriority priority)
        {
            Ready = onReady;
            return true;
        }
    }
}
