using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Nwn.Toolset.Avalonia.Appearances;

/// <summary>Observable presentation state for one realized appearance tile.</summary>
public sealed partial class AppearanceGalleryTile : ObservableObject
{
    public AppearanceGalleryOption Option { get; }
    public string Caption => Option.Caption;
    public string? Detail => Option.Detail;
    public bool HasDetail => !string.IsNullOrEmpty(Option.Detail);
    public double TileSize { get; }
    public double TileImageHeight => TileSize * 0.73;
    public string Glyph => Caption.Length > 0 ? Caption[..1].ToUpperInvariant() : "?";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPreview))]
    private Bitmap? _preview;

    [ObservableProperty]
    private bool _isCurrent;

    public bool PreviewRequested { get; internal set; }
    internal int PreviewAttempts { get; set; }
    internal long SnapshotVersion { get; set; }
    public bool HasPreview => Preview != null;

    public AppearanceGalleryTile(AppearanceGalleryOption option, bool isCurrent, double tileSize, long snapshotVersion)
    {
        Option = option ?? throw new ArgumentNullException(nameof(option));
        IsCurrent = isCurrent;
        TileSize = tileSize;
        SnapshotVersion = snapshotVersion;
    }
}
