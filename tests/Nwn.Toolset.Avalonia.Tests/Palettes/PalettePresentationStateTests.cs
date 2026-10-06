using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Resources;
using Nwn.Toolset.Avalonia.Palettes;
using Nwn.Toolset.Avalonia.Tests.Support;

namespace Nwn.Toolset.Avalonia.Tests.Palettes;

[TestClass]
public sealed class PalettePresentationStateTests
{
    [TestMethod]
    public void CategorySelectionProjectsDescendantsAndSearchRevealsMatches()
    {
        var actions = new PaletteActionsSpy();
        using var state = new PalettePresentationState(actions);
        var rootId = new PaletteCategoryId("root");
        var childId = new PaletteCategoryId("root/child");
        var firstId = new PaletteEntryId("utp/custom/first");
        var secondId = new PaletteEntryId("utp/custom/second");
        var root = Category(rootId, "Buildings", secondId, Category(childId, "Factories", firstId));
        var snapshot = Snapshot(root, Entry(firstId, "Reactor", "reactor", rootId, childId),
            Entry(secondId, "Relay", "relay", rootId));

        state.SetSnapshot(snapshot);
        Assert.AreEqual(1, state.Rows.Count, "Collapsed roots keep their children out of the virtualized tree.");
        state.SelectedRow = state.Rows[0];
        Assert.AreEqual(2, state.Tiles.Count, "Selecting a category includes entries in nested folders.");
        Assert.AreEqual(rootId, actions.SelectedCategories.Last());

        state.Query = "factories";
        Assert.AreEqual(1, state.CategoryMatches.Count);
        Assert.AreEqual("Buildings ›", state.CategoryMatches[0].ParentPath);
        Assert.AreEqual(0, state.Tiles.Count, "Blueprint text search is independent from category-name matches.");

        state.GoToCategoryCommand.Execute(state.CategoryMatches[0]);
        Assert.IsFalse(state.IsSearching);
        Assert.IsTrue(state.Rows.Any(row => row.Id == childId));
        Assert.AreEqual(childId, state.SelectedRow?.Id);
    }

    [TestMethod]
    public void TileEntriesIgnoreBlueprintSourceAndReadOnlyEntriesHaveNoWriteCapabilities()
    {
        var actions = new PaletteActionsSpy();
        using var state = new PalettePresentationState(actions);
        var categoryId = new PaletteCategoryId("tiles");
        var tileId = new PaletteEntryId("tile/grass");
        var standardId = new PaletteEntryId("utp/standard/crate");
        var category = new PaletteCategorySnapshot(categoryId, "Grass", 1, false, 0,
            Array.Empty<PaletteCategorySnapshot>(), new[] { tileId }, CategoryCapabilities());
        var tile = new PaletteEntrySnapshot(tileId, PaletteEntryKind.Tile, null, PaletteSource.Standard,
            "grass01", "Grass", "2 x 1 tiles", new[] { categoryId }, 2, 1,
            new[] { "grass01" }, EntryCapabilities(canPlace: true));
        var standard = Entry(standardId, "Crate", "crate", new[] { categoryId }, source: PaletteSource.Standard,
            capabilities: EntryCapabilities(canPlace: true, notice: "Base game content - read-only"));
        state.SetSnapshot(new PaletteSnapshot(2, PaletteMode.Tiles, null, PaletteSource.Custom,
            new[] { new PaletteTypeOption(null, "Tiles", "T", "New", null) },
            new[] { category }, new[] { tile, standard }, PaletteTilePaintMode.Manual, true, null,
            new PaletteCapabilities(true, true, true, true)));

        state.SelectedRow = state.Rows[0];
        Assert.AreEqual(1, state.Tiles.Count);
        Assert.IsTrue(state.Tiles[0].Snapshot.Capabilities.CanPlace);
        Assert.IsFalse(state.CanEditSelected);
        Assert.IsFalse(state.CanDeleteSelected);
        Assert.IsFalse(state.CanEditCopy);
    }

    [TestMethod]
    public void UnsupportedPreviewEntryDoesNotRequestOrReportAnImage()
    {
        var actions = new PaletteActionsSpy();
        using var state = new PalettePresentationState(actions);
        var categoryId = new PaletteCategoryId("tiles");
        var tileId = new PaletteEntryId("tile/eraser");
        var category = new PaletteCategorySnapshot(categoryId, "Eraser", 1, false, 0,
            Array.Empty<PaletteCategorySnapshot>(), new[] { tileId }, CategoryCapabilities());
        var eraser = new PaletteEntrySnapshot(tileId, PaletteEntryKind.Tile, null, PaletteSource.Custom,
            string.Empty, "Eraser", string.Empty, new[] { categoryId }, 1, 1,
            Array.Empty<string>(), EntryCapabilities(canPlace: true), SupportsPreview: false);
        state.SetSnapshot(new PaletteSnapshot(1, PaletteMode.Tiles, null, PaletteSource.Custom,
            Array.Empty<PaletteTypeOption>(), new[] { category }, new[] { eraser },
            PaletteTilePaintMode.Manual, true, null, new PaletteCapabilities(true, true, true, true)));
        state.SelectedRow = state.Rows[0];
        var row = state.Tiles.Single();

        state.EnsurePreview(row);

        Assert.AreEqual(0, actions.PreviewTokens.Count);
        Assert.IsFalse(row.PreviewRequested);
        Assert.IsFalse(row.HasPreview);
        Assert.IsNull(state.StatusMessage);
    }

    [TestMethod]
    public void SourceAndTilePaintSwitchesFollowTheirMutuallyExclusiveModesAndCapabilities()
    {
        var actions = new PaletteActionsSpy();
        using var state = new PalettePresentationState(actions);
        var blueprintCategoryId = new PaletteCategoryId("blueprints");
        var blueprintId = new PaletteEntryId("utp/custom/blueprint");
        var blueprintSnapshot = Snapshot(
            Category(blueprintCategoryId, "Blueprints", blueprintId),
            Entry(blueprintId, "Blueprint", "blueprint", blueprintCategoryId));

        state.SetSnapshot(blueprintSnapshot);

        Assert.IsTrue(state.IsBlueprintMode);
        Assert.IsTrue(state.ShowsSourceSwitch);
        Assert.IsFalse(state.ShowsTilePaintSwitch);
        Assert.IsTrue(state.IsCustomSource);
        Assert.IsFalse(state.IsStandardSource);

        state.SetSnapshot(blueprintSnapshot with { Revision = 2, Source = PaletteSource.Standard });

        Assert.IsTrue(state.ShowsSourceSwitch);
        Assert.IsTrue(state.IsStandardSource);
        Assert.IsFalse(state.IsCustomSource);

        var tileCategoryId = new PaletteCategoryId("tiles");
        var tileId = new PaletteEntryId("tiles/grass");
        var tileCategory = new PaletteCategorySnapshot(
            tileCategoryId,
            "Grass",
            1,
            false,
            0,
            Array.Empty<PaletteCategorySnapshot>(),
            new[] { tileId },
            CategoryCapabilities());
        var tile = new PaletteEntrySnapshot(
            tileId,
            PaletteEntryKind.Tile,
            null,
            PaletteSource.Standard,
            "grass01",
            "Grass",
            "2 x 1 tiles",
            new[] { tileCategoryId },
            2,
            1,
            new[] { "grass01" },
            EntryCapabilities(canPlace: true));
        var tileCapabilities = new PaletteCapabilities(true, true, true, true);
        var tileSnapshot = new PaletteSnapshot(
            3,
            PaletteMode.Tiles,
            null,
            PaletteSource.Custom,
            Array.Empty<PaletteTypeOption>(),
            new[] { tileCategory },
            new[] { tile },
            PaletteTilePaintMode.Auto,
            true,
            null,
            tileCapabilities);

        state.SetSnapshot(tileSnapshot);

        Assert.IsTrue(state.IsTileMode);
        Assert.IsFalse(state.ShowsSourceSwitch);
        Assert.IsTrue(state.ShowsTilePaintSwitch);
        Assert.IsTrue(state.IsAutoTilePaint);
        Assert.IsFalse(state.IsManualTilePaint);

        state.SetSnapshot(tileSnapshot with { Revision = 4, TilePaintMode = PaletteTilePaintMode.Manual });

        Assert.IsFalse(state.ShowsSourceSwitch);
        Assert.IsTrue(state.ShowsTilePaintSwitch);
        Assert.IsFalse(state.IsAutoTilePaint);
        Assert.IsTrue(state.IsManualTilePaint);

        var disabledCapabilities = new PaletteCapabilities(false, false, false, false);
        state.SetSnapshot(blueprintSnapshot with { Revision = 5, Capabilities = disabledCapabilities });
        Assert.IsFalse(state.ShowsSourceSwitch);
        Assert.IsFalse(state.ShowsTilePaintSwitch);

        state.SetSnapshot(tileSnapshot with { Revision = 6, Capabilities = disabledCapabilities });
        Assert.IsFalse(state.ShowsSourceSwitch);
        Assert.IsFalse(state.ShowsTilePaintSwitch);
    }
    [TestMethod]
    public void InitialSelectedCategoryIsExpandedAndMustBelongToSnapshot()
    {
        var actions = new PaletteActionsSpy();
        using var state = new PalettePresentationState(actions);
        var rootId = new PaletteCategoryId("root");
        var childId = new PaletteCategoryId("root/child");
        var entryId = new PaletteEntryId("utp/custom/entry");
        var root = Category(rootId, "Root", child: Category(childId, "Child", entryId));
        var snapshot = Snapshot(root, Entry(entryId, "Entry", "entry", rootId, childId)) with
        {
            InitialSelectedCategory = childId
        };

        state.SetSnapshot(snapshot);

        Assert.AreEqual(childId, state.SelectedRow?.Id);
        Assert.IsTrue(state.Rows.Any(row => row.Id == childId));
        Assert.AreEqual(1, state.Tiles.Count);

        var invalidSnapshot = snapshot with { InitialSelectedCategory = new PaletteCategoryId("missing") };
        Assert.ThrowsExactly<ArgumentException>(() => state.SetSnapshot(invalidSnapshot));
    }
    [TestMethod]
    public void SnapshotRestoresInitialLayoutPreferencesWithoutWritingThemBack()
    {
        var actions = new PaletteActionsSpy();
        using var state = new PalettePresentationState(actions);
        var categoryId = new PaletteCategoryId("root");
        var entryId = new PaletteEntryId("utp/custom/entry");
        var snapshot = Snapshot(Category(categoryId, "Root", entryId), Entry(entryId, "Entry", "entry", categoryId)) with
        {
            InitialTileSize = 180,
            CategoryProportion = 0.42
        };

        state.SetSnapshot(snapshot);

        Assert.AreEqual(180, state.TileSize);
        Assert.AreEqual(0.42, state.CategoryProportion);
        Assert.AreEqual(0, actions.TileSizes.Count);
        Assert.AreEqual(0, actions.CategoryProportions.Count);
    }

    [TestMethod]
    public async Task SnapshotChangeCancelsOutstandingLazyPreviewRequest()
    {
        var actions = new PaletteActionsSpy
        {
            PreviewCompletion = new TaskCompletionSource< global::Avalonia.Media.Imaging.Bitmap?>(
                TaskCreationOptions.RunContinuationsAsynchronously)
        };
        using var state = new PalettePresentationState(actions);
        var categoryId = new PaletteCategoryId("root");
        var entryId = new PaletteEntryId("utp/custom/one");
        var entry = Entry(entryId, "One", "one", categoryId);
        state.SetSnapshot(Snapshot(Category(categoryId, "Root", entryId), entry));
        state.SelectedRow = state.Rows[0];
        var oldRow = state.Tiles[0];
        state.EnsurePreview(oldRow);
        var token = actions.PreviewToken;
        Assert.IsTrue(token.HasValue);
        Assert.IsFalse(token.Value.IsCancellationRequested);

        state.SetSnapshot(Snapshot(Category(categoryId, "Root", entryId), new[] { entry }, revision: 2));
        Assert.IsTrue(token.Value.IsCancellationRequested);
        actions.PreviewCompletion.SetResult(null);
        await Task.Delay(20);
        Assert.IsNull(state.Tiles[0].Preview);
    }

    [TestMethod]
    public async Task NullPreviewResultSettlesAsUnavailableWithoutAutomaticRetryAsync()
    {
        var actions = new PaletteActionsSpy
        {
            PreviewCompletion = new TaskCompletionSource<global::Avalonia.Media.Imaging.Bitmap?>(
                TaskCreationOptions.RunContinuationsAsynchronously)
        };

        await GraphTestRuntime.RunAsync(() => new global::Avalonia.Controls.Control(), _ =>
        {
            using var state = new PalettePresentationState(actions);
            var categoryId = new PaletteCategoryId("root");
            var entryId = new PaletteEntryId("utp/custom/one");
            var entry = Entry(entryId, "Entry", "one", categoryId);
            state.SetSnapshot(Snapshot(Category(categoryId, "Root", entryId), entry) with
            {
                StatusMessage = "Tile placement instructions.",
            });
            state.SelectedRow = state.Rows[0];
            var row = state.Tiles[0];
            var statusChangedOnUiThread = true;
            state.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(PalettePresentationState.StatusMessage))
                {
                    statusChangedOnUiThread &= global::Avalonia.Threading.Dispatcher.UIThread.CheckAccess();
                }
            };

            state.EnsurePreview(row);
            Assert.IsTrue(row.PreviewRequested);

            actions.PreviewCompletion.SetResult(null);
            global::Avalonia.Threading.Dispatcher.UIThread.RunJobs();

            Assert.IsFalse(row.PreviewRequested);
            Assert.IsNull(row.Preview);
            Assert.AreEqual(
                state.Texts.Get(PaletteStringId.PreviewUnavailable, entry.Name),
                state.StatusMessage);
            Assert.IsTrue(statusChangedOnUiThread,
                "Preview completion updates observable status on the host UI dispatcher.");
            Assert.AreEqual(1, actions.PreviewTokens.Count,
                "A failed render settles the visible request instead of automatically retrying forever.");
            state.EnsurePreview(row);
            Assert.AreEqual(1, actions.PreviewTokens.Count,
                "A realized failed row stays settled until explicit invalidation or a new snapshot revision.");

            state.SetSnapshot(Snapshot(Category(categoryId, "Root", entryId), new[] { entry }, revision: 2) with
            {
                StatusMessage = "Updated tile guidance.",
            });
            Assert.AreEqual("Updated tile guidance.", state.StatusMessage,
                "A new snapshot clears the prior local failure message.");
            state.EnsurePreview(state.Tiles[0]);
            Assert.AreEqual(2, actions.PreviewTokens.Count,
                "A new snapshot revision allows a fresh preview request.");
            Assert.IsTrue(statusChangedOnUiThread);
        });
    }

    [TestMethod]
    public void BlueprintCategoryBrowsingIsUncapped()
    {
        var actions = new PaletteActionsSpy();
        using var state = new PalettePresentationState(actions);
        var categoryId = new PaletteCategoryId("root");
        var entries = Enumerable.Range(0, 250)
            .Select(index => Entry(new PaletteEntryId($"utp/custom/item-{index:D3}"),
                $"Item {index:D3}", $"item-{index:D3}", categoryId))
            .ToArray();
        var category = new PaletteCategorySnapshot(categoryId, "Root", entries.Length, false, 0,
            Array.Empty<PaletteCategorySnapshot>(), entries.Select(entry => entry.Id).ToArray(), CategoryCapabilities());

        state.SetSnapshot(Snapshot(category, entries));
        state.SelectedRow = state.Rows[0];

        Assert.AreEqual(250, state.Tiles.Count,
            "Opening a category keeps virtualized browsing over its full blueprint list.");
    }

    [TestMethod]
    public void TileSearchStaysInsideTheSelectedTileCategory()
    {
        var actions = new PaletteActionsSpy();
        using var state = new PalettePresentationState(actions);
        var firstCategoryId = new PaletteCategoryId("tiles/first");
        var secondCategoryId = new PaletteCategoryId("tiles/second");
        var firstId = new PaletteEntryId("tile/first");
        var secondId = new PaletteEntryId("tile/second");
        var firstCategory = new PaletteCategorySnapshot(firstCategoryId, "Floor", 1, false, 0,
            Array.Empty<PaletteCategorySnapshot>(), new[] { firstId }, CategoryCapabilities());
        var secondCategory = new PaletteCategorySnapshot(secondCategoryId, "Wall", 1, false, 1,
            Array.Empty<PaletteCategorySnapshot>(), new[] { secondId }, CategoryCapabilities());
        var first = new PaletteEntrySnapshot(firstId, PaletteEntryKind.Tile, null, PaletteSource.Custom,
            "floor_a", "Floor A", string.Empty, new[] { firstCategoryId }, 1, 1,
            Array.Empty<string>(), EntryCapabilities(canPlace: true));
        var second = new PaletteEntrySnapshot(secondId, PaletteEntryKind.Tile, null, PaletteSource.Custom,
            "wall_target", "Target Wall", string.Empty, new[] { secondCategoryId }, 1, 1,
            Array.Empty<string>(), EntryCapabilities(canPlace: true));
        var snapshot = new PaletteSnapshot(1, PaletteMode.Tiles, null, PaletteSource.Custom,
            new[] { new PaletteTypeOption(null, "Tiles", "T", "New", null) },
            new[] { firstCategory, secondCategory }, new[] { first, second },
            PaletteTilePaintMode.Auto, true, null, new PaletteCapabilities(false, false, false, false),
            InitialSelectedCategory: firstCategoryId);

        state.SetSnapshot(snapshot);
        state.Query = "target";
        Assert.AreEqual(0, state.Tiles.Count, "Search does not jump to tiles from another category.");

        state.Query = string.Empty;
        state.SelectedRow = state.Rows.Single(row => row.Id == secondCategoryId);
        state.Query = "target";
        Assert.AreEqual(secondId, state.Tiles.Single().Id);
    }

    [TestMethod]
    public void RebuildKeepsPreviewRequestedByNewlyRealizedRow()
    {
        var actions = new PaletteActionsSpy
        {
            PreviewCompletion = new TaskCompletionSource<global::Avalonia.Media.Imaging.Bitmap?>(
                TaskCreationOptions.RunContinuationsAsynchronously)
        };
        using var state = new PalettePresentationState(actions);
        var categoryId = new PaletteCategoryId("root");
        var entryId = new PaletteEntryId("utp/custom/entry");
        var entry = Entry(entryId, "Entry", "entry", categoryId);
        state.Tiles.CollectionChanged += (_, args) =>
        {
            if (args.NewItems is { Count: > 0 } && args.NewItems[0] is PaletteEntryRow row)
            {
                state.EnsurePreview(row);
            }
        };

        state.SetSnapshot(Snapshot(Category(categoryId, "Root", entryId), entry)
            with { InitialSelectedCategory = categoryId });

        Assert.AreEqual(1, actions.PreviewTokens.Count);
        Assert.IsFalse(actions.PreviewTokens[0].IsCancellationRequested,
            "The snapshot must not cancel a request started as the new row enters the realized collection.");
        actions.PreviewCompletion.SetResult(null);
        state.Dispose();
    }

    [TestMethod]
    public async Task DisposingAfterPreviewCompletionBeforeDispatcherResumeIsSafeAsync()
    {
        var actions = new PaletteActionsSpy
        {
            PreviewCompletion = new TaskCompletionSource<global::Avalonia.Media.Imaging.Bitmap?>(
                TaskCreationOptions.RunContinuationsAsynchronously)
        };

        await GraphTestRuntime.RunAsync(() => new global::Avalonia.Controls.Control(), _ =>
        {
            using var state = new PalettePresentationState(actions);
            var categoryId = new PaletteCategoryId("root");
            var entryId = new PaletteEntryId("utp/custom/entry");
            var entry = Entry(entryId, "Entry", "entry", categoryId);
            state.SetSnapshot(Snapshot(Category(categoryId, "Root", entryId), entry)
                with { InitialSelectedCategory = categoryId });
            state.SelectedRow = state.Rows.Single(row => row.Id == categoryId);
            var row = state.Tiles.Single();
            var statusChanged = false;
            state.PropertyChanged += (_, args) =>
            {
                statusChanged |= args.PropertyName == nameof(PalettePresentationState.StatusMessage);
            };

            state.EnsurePreview(row);
            Assert.IsTrue(row.PreviewRequested);

            actions.PreviewCompletion.SetResult(null);
            state.Dispose();
            global::Avalonia.Threading.Dispatcher.UIThread.RunJobs();

            Assert.IsTrue(row.PreviewRequested,
                "A completion queued before cancellation must not mutate a disposed palette state.");
            Assert.IsNull(row.Preview);
            Assert.IsNull(state.StatusMessage);
            Assert.IsFalse(statusChanged,
                "The queued completion cannot publish status after disposal has canceled its request.");
        });
    }

    [TestMethod]
    public void QueryRebuildCancelsPriorRequestAndRejectsTheReplacedRow()
    {
        var actions = new PaletteActionsSpy
        {
            PreviewCompletion = new TaskCompletionSource<global::Avalonia.Media.Imaging.Bitmap?>(
                TaskCreationOptions.RunContinuationsAsynchronously)
        };
        using var state = new PalettePresentationState(actions);
        var categoryId = new PaletteCategoryId("root");
        var firstId = new PaletteEntryId("utp/custom/first");
        var secondId = new PaletteEntryId("utp/custom/second");
        var category = new PaletteCategorySnapshot(categoryId, "Root", 2, false, 0,
            Array.Empty<PaletteCategorySnapshot>(), new[] { firstId, secondId }, CategoryCapabilities());
        state.SetSnapshot(Snapshot(category,
            Entry(firstId, "First", "first", categoryId),
            Entry(secondId, "Second", "second", categoryId)));
        state.SelectedRow = state.Rows[0];
        var replacedRow = state.Tiles[0];
        state.EnsurePreview(replacedRow);
        var oldToken = actions.PreviewTokens[0];

        state.Tiles.CollectionChanged += (_, args) =>
        {
            if (args.NewItems is { Count: > 0 } && args.NewItems[0] is PaletteEntryRow row)
            {
                state.EnsurePreview(row);
            }
        };
        state.Query = "Second";

        Assert.IsTrue(oldToken.IsCancellationRequested);
        Assert.IsTrue(actions.PreviewTokens.Count >= 2);
        Assert.IsTrue(actions.PreviewTokens.Take(actions.PreviewTokens.Count - 1)
            .All(token => token.IsCancellationRequested));
        Assert.IsFalse(actions.PreviewTokens[^1].IsCancellationRequested);
        Assert.AreEqual(secondId, state.Tiles.Single().Id);
        Assert.IsFalse(state.Tiles.Contains(replacedRow), "A completed request for the replaced row cannot update the current projection.");
        actions.PreviewCompletion.SetResult(null);
    }

    [TestMethod]
    public void InvalidatingVisibleEntryRequestsAFreshPreview()
    {
        var actions = new PaletteActionsSpy
        {
            PreviewCompletion = new TaskCompletionSource<global::Avalonia.Media.Imaging.Bitmap?>(
                TaskCreationOptions.RunContinuationsAsynchronously)
        };
        using var state = new PalettePresentationState(actions);
        var categoryId = new PaletteCategoryId("root");
        var entryId = new PaletteEntryId("utp/custom/entry");
        state.SetSnapshot(Snapshot(Category(categoryId, "Root", entryId),
            Entry(entryId, "Entry", "entry", categoryId)));
        state.SelectedRow = state.Rows[0];
        var row = state.Tiles[0];
        state.EnsurePreview(row);
        Assert.IsTrue(row.PreviewRequested);

        state.InvalidatePreview(entryId);

        Assert.AreEqual(2, actions.PreviewTokens.Count);
        Assert.IsTrue(actions.PreviewTokens[0].IsCancellationRequested);
        Assert.IsFalse(actions.PreviewTokens[1].IsCancellationRequested);
        Assert.IsTrue(row.PreviewRequested);
        actions.PreviewCompletion.SetResult(null);
    }

    [TestMethod]
    public async Task SupportedPreviewReturningNullIsReportedAsUnavailable()
    {
        var actions = new PaletteActionsSpy
        {
            PreviewCompletion = new TaskCompletionSource<global::Avalonia.Media.Imaging.Bitmap?>(
                TaskCreationOptions.RunContinuationsAsynchronously)
        };
        using var state = new PalettePresentationState(actions);
        var categoryId = new PaletteCategoryId("root");
        var entryId = new PaletteEntryId("utp/custom/entry");
        state.SetSnapshot(Snapshot(Category(categoryId, "Root", entryId),
            Entry(entryId, "Entry", "entry", categoryId)));
        state.SelectedRow = state.Rows[0];
        var row = state.Tiles.Single();
        var statusChanged = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        state.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(state.StatusMessage) && state.StatusMessage is not null)
            {
                statusChanged.TrySetResult();
            }
        };

        state.EnsurePreview(row);
        Assert.IsTrue(row.PreviewRequested);
        Assert.AreEqual(1, actions.PreviewTokens.Count);
        actions.PreviewCompletion.SetResult(null);
        await statusChanged.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.IsFalse(row.PreviewRequested);
        Assert.IsFalse(row.HasPreview);
        StringAssert.Contains(state.StatusMessage!, "Entry");
        state.EnsurePreview(row);
        Assert.AreEqual(1, actions.PreviewTokens.Count, "A failed model preview must not retry continuously.");
    }

    [TestMethod]
    public async Task ExpectedPreviewFailureIsVisibleAndCanBeRetried()
    {
        var actions = new PaletteActionsSpy
        {
            PreviewFailure = new IOException("unreadable source"),
            PreviewCompletion = new TaskCompletionSource<global::Avalonia.Media.Imaging.Bitmap?>(
                TaskCreationOptions.RunContinuationsAsynchronously)
        };
        using var state = new PalettePresentationState(actions);
        var categoryId = new PaletteCategoryId("root");
        var entryId = new PaletteEntryId("utp/custom/entry");
        state.SetSnapshot(Snapshot(Category(categoryId, "Root", entryId),
            Entry(entryId, "Entry", "entry", categoryId)));
        state.SelectedRow = state.Rows[0];
        var row = state.Tiles[0];
        state.EnsurePreview(row);
        await Task.Delay(20);

        Assert.IsFalse(row.PreviewRequested);
        StringAssert.Contains(state.StatusMessage!, "Entry");

        actions.PreviewFailure = null;
        state.InvalidatePreview(entryId);
        Assert.IsTrue(row.PreviewRequested);
        Assert.IsNull(state.StatusMessage);
        Assert.AreEqual(2, actions.PreviewTokens.Count);
        actions.PreviewCompletion.SetResult(null);
    }

    private static PaletteSnapshot Snapshot(
        PaletteCategorySnapshot category,
        params PaletteEntrySnapshot[] entries) => Snapshot(category, entries, revision: 1);

    private static PaletteSnapshot Snapshot(
        PaletteCategorySnapshot category,
        IReadOnlyList<PaletteEntrySnapshot> entries,
        long revision) => new(
        revision,
        PaletteMode.Blueprints,
        ModuleResourceType.Utp,
        PaletteSource.Custom,
        new[] { new PaletteTypeOption(ModuleResourceType.Utp, "Placeables", "P", "New Placeable...", null) },
        new[] { category },
        entries,
        PaletteTilePaintMode.Auto,
        true,
        null,
        new PaletteCapabilities(true, true, true, true));

    private static PaletteCategorySnapshot Category(
        PaletteCategoryId id,
        string name,
        PaletteEntryId? directEntry = null,
        PaletteCategorySnapshot? child = null) => new(
        id,
        name,
        directEntry is null ? child?.Count ?? 0 : child is null ? 1 : child.Count + 1,
        false,
        0,
        child is null ? Array.Empty<PaletteCategorySnapshot>() : new[] { child },
        directEntry is null ? Array.Empty<PaletteEntryId>() : new[] { directEntry.Value },
        CategoryCapabilities());

    private static PaletteEntrySnapshot Entry(
        PaletteEntryId id,
        string name,
        string resRef,
        params PaletteCategoryId[] categories) => Entry(id, name, resRef, categories, PaletteSource.Custom, EntryCapabilities(true));

    private static PaletteEntrySnapshot Entry(
        PaletteEntryId id,
        string name,
        string resRef,
        IReadOnlyList<PaletteCategoryId> categories,
        PaletteSource source,
        PaletteEntryCapabilities capabilities) => new(
        id,
        PaletteEntryKind.Blueprint,
        ModuleResourceType.Utp,
        source,
        resRef,
        name,
        resRef,
        categories,
        null,
        null,
        Array.Empty<string>(),
        capabilities);

    private static PaletteCategoryCapabilities CategoryCapabilities() => new(false, false, false, false, false, false, null);

    private static PaletteEntryCapabilities EntryCapabilities(bool canPlace, string? notice = null) =>
        new(canPlace, false, false, false, notice);
}
