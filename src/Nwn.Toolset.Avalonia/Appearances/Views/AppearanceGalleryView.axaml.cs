using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

namespace Nwn.Toolset.Avalonia.Appearances.Views;

/// <summary>Shared searchable appearance gallery view.</summary>
public partial class AppearanceGalleryView : UserControl
{
    private const double LoadAheadPixels = 500;

    public AppearanceGalleryView()
    {
        InitializeComponent();
    }

    private void OnTileLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: AppearanceGalleryTile tile } &&
            DataContext is AppearanceGalleryViewModel gallery)
            gallery.EnsurePreview(tile);
    }

    private void OnGalleryScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (sender is not Control control ||
            control.DataContext is not AppearanceGalleryViewModel gallery ||
            !gallery.CanLoadMore || e.OffsetDelta.Y <= 0)
            return;
        var scrollViewer = control as ScrollViewer ?? control.FindDescendantOfType<ScrollViewer>();
        if (scrollViewer == null)
            return;
        var remaining = scrollViewer.Extent.Height - scrollViewer.Offset.Y - scrollViewer.Viewport.Height;
        if (remaining <= LoadAheadPixels)
            gallery.LoadMoreCommand.Execute(null);
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
