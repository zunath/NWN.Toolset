using Avalonia.Media.Imaging;
using Nwn.Toolset.Avalonia.Palettes;

namespace Nwn.Toolset.Avalonia.Tests.Support;

internal sealed class PaletteActionsSpy : IPaletteActions
{
    public List<PaletteCategoryId?> SelectedCategories { get; } = new();

    public PaletteEntrySnapshot? PlacedEntry { get; private set; }

    public CancellationToken? PreviewToken { get; private set; }

    public List<CancellationToken> PreviewTokens { get; } = new();

    public TaskCompletionSource<Bitmap?>? PreviewCompletion { get; set; }

    public Exception? PreviewFailure { get; set; }

    public List<double> TileSizes { get; } = new();

    public List<double> CategoryProportions { get; } = new();

    public void SelectType(PaletteTypeOption type) { }
    public void SelectSource(PaletteSource source) { }
    public void SelectMode(PaletteMode mode) { }
    public void SelectTilePaintMode(PaletteTilePaintMode mode) { }
    public void SelectCategory(PaletteCategoryId? categoryId) => SelectedCategories.Add(categoryId);
    public void SetTileSize(double tileSize) => TileSizes.Add(tileSize);
    public void SetCategoryProportion(double categoryProportion) => CategoryProportions.Add(categoryProportion);
    public void Place(PaletteEntrySnapshot entry) => PlacedEntry = entry;
    public void Edit(PaletteEntrySnapshot entry) { }
    public void EditCopy(PaletteEntrySnapshot entry) { }
    public Task DeleteAsync(PaletteEntrySnapshot entry, CancellationToken cancellationToken) => Task.CompletedTask;
    public void NewBlueprint(PaletteCategoryId? categoryId) { }
    public Task NewCategoryAsync(PaletteCategoryId? categoryId, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task RenameCategoryAsync(PaletteCategoryId categoryId, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task DeleteCategoryAsync(PaletteCategoryId categoryId, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task TogglePinAsync(PaletteCategoryId categoryId, CancellationToken cancellationToken) => Task.CompletedTask;
    public void FileSelectedEntry(PaletteCategoryId categoryId) { }

    public ValueTask<Bitmap?> LoadPreviewAsync(PaletteEntrySnapshot entry, CancellationToken cancellationToken)
    {
        PreviewToken = cancellationToken;
        PreviewTokens.Add(cancellationToken);
        if (PreviewFailure is { } failure)
        {
            return new ValueTask<Bitmap?>(Task.FromException<Bitmap?>(failure));
        }

        return new ValueTask<Bitmap?>(PreviewCompletion?.Task ?? Task.FromResult<Bitmap?>(null));
    }
}
