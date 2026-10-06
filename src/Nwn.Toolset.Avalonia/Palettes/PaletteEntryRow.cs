using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Nwn.Toolset.Avalonia.Palettes;

/// <summary>Mutable presentation state for an immutable palette entry.</summary>
public partial class PaletteEntryRow : ObservableObject
{
    public PaletteEntryRow(PaletteEntrySnapshot snapshot, long snapshotRevision)
    {
        Snapshot = snapshot;
        SnapshotRevision = snapshotRevision;
    }

    public PaletteEntrySnapshot Snapshot { get; }

    public PaletteEntryId Id => Snapshot.Id;

    public PaletteEntryKind Kind => Snapshot.Kind;

    public string ResRef => Snapshot.ResRef;

    public string Name => Snapshot.Name;

    public string Subtitle => Snapshot.Subtitle;

    public bool IsTile => Snapshot.Kind == PaletteEntryKind.Tile;

    public bool HasCategoryPath => !string.IsNullOrEmpty(CategoryPath);

    public string? CategoryPath { get; init; }

    public long SnapshotRevision { get; }

    public bool PreviewRequested { get; set; }

    public bool HasPreview => Preview is not null;

    [ObservableProperty]
    private Bitmap? _preview;

    partial void OnPreviewChanged(Bitmap? value) => OnPropertyChanged(nameof(HasPreview));

    public string Glyph => string.IsNullOrWhiteSpace(Name) ? "?" : Name.Trim()[..1].ToUpperInvariant();
}
