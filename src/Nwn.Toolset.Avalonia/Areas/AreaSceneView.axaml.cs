// SPDX-License-Identifier: MIT
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Nwn.Preview.Areas;
using Nwn.Toolset.Avalonia.Localization;

namespace Nwn.Toolset.Avalonia.Areas;

/// <summary>Scene surface with notices, drag readout and manipulation pad.</summary>
public sealed partial class AreaSceneView : UserControl
{
    public static readonly StyledProperty<AreaSceneOverlay> OverlayProperty =
        AvaloniaProperty.Register<AreaSceneView, AreaSceneOverlay>(nameof(Overlay), new());
    public static readonly StyledProperty<bool> IsDraggingProperty =
        AvaloniaProperty.Register<AreaSceneView, bool>(nameof(IsDragging));
    public static readonly StyledProperty<string> DragPositionProperty =
        AvaloniaProperty.Register<AreaSceneView, string>(nameof(DragPosition), string.Empty);
    public static readonly StyledProperty<string> DragFacingProperty =
        AvaloniaProperty.Register<AreaSceneView, string>(nameof(DragFacing), string.Empty);
    public static readonly StyledProperty<string> DragDeltaProperty =
        AvaloniaProperty.Register<AreaSceneView, string>(nameof(DragDelta), string.Empty);
    public static readonly StyledProperty<bool> HasDragDeltaProperty =
        AvaloniaProperty.Register<AreaSceneView, bool>(nameof(HasDragDelta));

    private readonly AreaEditorTexts _texts;
    private bool _rotateHasRepeated;
    public AreaEditorSurface Surface { get; } = new();
    public AreaViewportControl Viewport => Surface.Viewport;
    public AreaCameraControls CameraControls { get; }
    public Button RaiseTileButton { get; }
    public Button LowerTileButton { get; }
    public ContextMenu? SurfaceContextMenu { get => Surface.ContextMenu; set => Surface.ContextMenu = value; }
    public AreaSceneOverlay Overlay { get => GetValue(OverlayProperty); set => SetValue(OverlayProperty, value); }
    public bool IsDragging { get => GetValue(IsDraggingProperty); private set => SetValue(IsDraggingProperty, value); }
    public string DragPosition { get => GetValue(DragPositionProperty); private set => SetValue(DragPositionProperty, value); }
    public string DragFacing { get => GetValue(DragFacingProperty); private set => SetValue(DragFacingProperty, value); }
    public string DragDelta { get => GetValue(DragDeltaProperty); private set => SetValue(DragDeltaProperty, value); }
    public bool HasDragDelta { get => GetValue(HasDragDeltaProperty); private set => SetValue(HasDragDeltaProperty, value); }
    public string DragHintText => _texts.Get(AreaEditorStringId.DragHint);
    public string TileHeightHintText => _texts.Get(AreaEditorStringId.TileHeightHint);
    public string RotateAnticlockwiseText => _texts.Get(AreaEditorStringId.RotateAnticlockwise);
    public string RotateClockwiseText => _texts.Get(AreaEditorStringId.RotateClockwise);
    public string RotateRandomlyText => _texts.Get(AreaEditorStringId.RotateRandomly);
    public string RaiseTileText => _texts.Get(AreaEditorStringId.RaiseTile);
    public string LowerTileText => _texts.Get(AreaEditorStringId.LowerTile);
    public event EventHandler? RaiseTileRequested;
    public event EventHandler? LowerTileRequested;

    public AreaSceneView() : this(AreaEditorTexts.English) { }

    public AreaSceneView(AreaEditorTexts texts)
    {
        ArgumentNullException.ThrowIfNull(texts);
        _texts = texts;
        AvaloniaXamlLoader.Load(this);
        this.FindControl<Grid>(nameof(SceneRoot))!.DataContext = this;
        CameraControls = this.FindControl<AreaCameraControls>(nameof(CameraPad))!;
        CameraControls.Viewport = Viewport;
        RaiseTileButton = this.FindControl<Button>(nameof(RaiseTileAction))!;
        LowerTileButton = this.FindControl<Button>(nameof(LowerTileAction))!;
        Viewport.ManipulationPreviewChanged += ShowDragReadout;
        Viewport.RenderStatusChanged += OnGlRenderStatusChanged;
    }

    private void OnRotateSelectionClockwise(object? sender, RoutedEventArgs args) => RotateSelectionTick(-1f);
    private void OnRotateSelectionAnticlockwise(object? sender, RoutedEventArgs args) => RotateSelectionTick(1f);
    private void RotateSelectionTick(float direction)
    {
        if (!Overlay.CanRotateSelection) return;
        Viewport.NudgeSelectedRotation(direction, isFirstStep: !_rotateHasRepeated);
        _rotateHasRepeated = true;
    }
    private void OnRotateSelectionReleased(object? sender, PointerReleasedEventArgs args) => EndRotateSelection();
    // Capture loss commits the same one edit as release when the pointer leaves the button.
    private void OnRotateSelectionCaptureLost(object? sender, PointerCaptureLostEventArgs args) => EndRotateSelection();
    private void EndRotateSelection()
    {
        _rotateHasRepeated = false;
        Viewport.CommitSelectedRotation();
    }
    private void OnRotateSelectionRandomly(object? sender, RoutedEventArgs args)
    {
        if (Overlay.CanRotateSelection) Viewport.RandomizeSelectedRotation();
    }
    private void OnRaiseTile(object? sender, RoutedEventArgs args)
    {
        if (Overlay.HasTileSelection) RaiseTileRequested?.Invoke(this, EventArgs.Empty);
    }
    private void OnLowerTile(object? sender, RoutedEventArgs args)
    {
        if (Overlay.HasTileSelection) LowerTileRequested?.Invoke(this, EventArgs.Empty);
    }
    private void OnGlRenderStatusChanged(object? sender, string message)
    {
        var border = this.FindControl<Border>(nameof(GlStatusBorder))!;
        border.IsVisible = !string.IsNullOrEmpty(message);
        this.FindControl<TextBlock>(nameof(GlStatusText))!.Text = message;
    }

    private void ShowDragReadout(InstanceMarker? original, InstanceMarker? preview)
    {
        if (original is null || preview is null) { IsDragging = false; return; }
        DragPosition = _texts.Get(AreaEditorStringId.DragPosition, preview.Position.X, preview.Position.Y, preview.Position.Z);
        var headingDegrees = MathF.Atan2(preview.Orientation.Y, preview.Orientation.X) * 180f / MathF.PI;
        if (headingDegrees < 0) headingDegrees += 360f;
        DragFacing = _texts.Get(AreaEditorStringId.DragFacing, headingDegrees);
        var moved = Vector3.Distance(preview.Position, original.Position);
        var turned = MathF.Abs(MathF.Atan2(preview.Orientation.Y, preview.Orientation.X) -
            MathF.Atan2(original.Orientation.Y, original.Orientation.X)) * 180f / MathF.PI;
        DragDelta = moved > 1e-4f ? _texts.Get(AreaEditorStringId.DragMoved, moved)
            : turned > 1e-4f ? _texts.Get(AreaEditorStringId.DragTurned, turned) : string.Empty;
        HasDragDelta = !string.IsNullOrEmpty(DragDelta);
        IsDragging = true;
    }
}
