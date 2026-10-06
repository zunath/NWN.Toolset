using Avalonia.Media.Imaging;

namespace Nwn.Toolset.Avalonia.Palettes;

/// <summary>Host-owned palette operations and preview resolution.</summary>
public interface IPaletteActions
{
    void SelectType(PaletteTypeOption type);

    void SelectSource(PaletteSource source);

    void SelectMode(PaletteMode mode);

    void SelectTilePaintMode(PaletteTilePaintMode mode);

    void SelectCategory(PaletteCategoryId? categoryId);

    void SetTileSize(double tileSize);

    void SetCategoryProportion(double categoryProportion);

    void Place(PaletteEntrySnapshot entry);

    void Edit(PaletteEntrySnapshot entry);

    void EditCopy(PaletteEntrySnapshot entry);

    Task DeleteAsync(PaletteEntrySnapshot entry, CancellationToken cancellationToken);

    void NewBlueprint(PaletteCategoryId? categoryId);

    Task NewCategoryAsync(PaletteCategoryId? categoryId, CancellationToken cancellationToken);

    Task RenameCategoryAsync(PaletteCategoryId categoryId, CancellationToken cancellationToken);

    Task DeleteCategoryAsync(PaletteCategoryId categoryId, CancellationToken cancellationToken);

    Task TogglePinAsync(PaletteCategoryId categoryId, CancellationToken cancellationToken);

    void FileSelectedEntry(PaletteCategoryId categoryId);

    ValueTask<Bitmap?> LoadPreviewAsync(PaletteEntrySnapshot entry, CancellationToken cancellationToken);
}
