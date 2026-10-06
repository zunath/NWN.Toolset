using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Nwn.Preview.Pixels;
using Nwn.Preview.Scene;

namespace Nwn.Toolset.Avalonia.Viewport;

/// <summary>A reusable textured static-model viewport with orbit, pan, and zoom input.</summary>
public sealed class ModelPreviewControl : UserControl
{
    private readonly ModelViewportSurface _surface = new();
    private readonly Border _inputSurface;
    private global::Avalonia.Point _lastPointer;
    private MouseButton _dragButton;

    public ModelPreviewControl()
    {
        var grid = new Grid();
        grid.Children.Add(_surface);
        _inputSurface = new Border { Background = Brushes.Transparent };
        _inputSurface.PointerPressed += OnPointerPressed;
        _inputSurface.PointerMoved += OnPointerMoved;
        _inputSurface.PointerReleased += OnPointerReleased;
        _inputSurface.PointerWheelChanged += OnPointerWheelChanged;
        grid.Children.Add(_inputSurface);
        Content = grid;
    }

    public PreparedScene? Scene
    {
        get => _surface.Scene;
        set => _surface.SetScene(value);
    }

    public IReadOnlyDictionary<string, RgbaImage> Textures
    {
        get => _surface.Textures;
        set => _surface.SetTextures(value);
    }

    public ModelViewportRenderObservation? LastSuccessfulRenderObservation => _surface.LastSuccessfulRenderObservation;

    private void OnPointerPressed(object? sender, PointerPressedEventArgs args)
    {
        var properties = args.GetCurrentPoint(this).Properties;
        _dragButton = properties.IsRightButtonPressed ? MouseButton.Right : MouseButton.Left;
        _lastPointer = args.GetPosition(this);
        args.Pointer.Capture(_inputSurface);
        args.Handled = true;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs args)
    {
        if (args.Pointer.Captured != _inputSurface)
            return;
        var point = args.GetPosition(this);
        var dx = (float)(point.X - _lastPointer.X);
        var dy = (float)(point.Y - _lastPointer.Y);
        if (_dragButton == MouseButton.Right)
            _surface.Camera.Pan(dx, -dy);
        else
            _surface.Camera.Orbit(-dx, dy);
        _lastPointer = point;
        _surface.RequestNextFrameRendering();
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs args)
    {
        args.Pointer.Capture(null);
        _dragButton = MouseButton.None;
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs args)
    {
        _surface.Camera.Zoom((float)args.Delta.Y);
        _surface.RequestNextFrameRendering();
        args.Handled = true;
    }

    private enum MouseButton
    {
        None,
        Left,
        Right
    }
}
