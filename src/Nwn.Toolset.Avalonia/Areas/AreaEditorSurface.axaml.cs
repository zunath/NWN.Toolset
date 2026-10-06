using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace Nwn.Toolset.Avalonia.Areas;

/// <summary>Reusable area viewport and hit-test surface; hosts own labels, menus and game policy.</summary>
public sealed partial class AreaEditorSurface : UserControl
{
    public AreaViewportControl Viewport { get; }

    public AreaEditorSurface()
    {
        AvaloniaXamlLoader.Load(this);
        Viewport = this.FindControl<AreaViewportControl>("ViewportControl")
            ?? throw new InvalidOperationException("Area viewport surface is missing its viewport control.");
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e) => Viewport.HandlePointerPressed(e);

    private void OnPointerMoved(object? sender, PointerEventArgs e) => Viewport.HandlePointerMoved(e);

    private void OnPointerExited(object? sender, PointerEventArgs e) => Viewport.HandlePointerExited(e);

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e) => Viewport.HandlePointerReleased(e);

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e) => Viewport.HandlePointerWheel(e);
}
