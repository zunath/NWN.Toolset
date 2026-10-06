// SPDX-License-Identifier: MIT

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Nwn.Toolset.Avalonia.Localization;

namespace Nwn.Toolset.Avalonia.Areas;

/// <summary>Camera pad that operates the viewport owned by the current area.</summary>
public sealed partial class AreaCameraControls : UserControl
{
    private readonly AreaEditorTexts _texts;
    public AreaViewportControl? Viewport { get; set; }
    public string PanLeftText => _texts.Get(AreaEditorStringId.PanLeft);
    public string PanRightText => _texts.Get(AreaEditorStringId.PanRight);
    public string PanUpText => _texts.Get(AreaEditorStringId.PanUp);
    public string PanDownText => _texts.Get(AreaEditorStringId.PanDown);
    public string OrbitLeftText => _texts.Get(AreaEditorStringId.OrbitLeft);
    public string OrbitRightText => _texts.Get(AreaEditorStringId.OrbitRight);
    public string OrbitUpText => _texts.Get(AreaEditorStringId.OrbitUp);
    public string OrbitDownText => _texts.Get(AreaEditorStringId.OrbitDown);
    public string ZoomInText => _texts.Get(AreaEditorStringId.ZoomIn);
    public string ZoomOutText => _texts.Get(AreaEditorStringId.ZoomOut);
    public string ReorientText => _texts.Get(AreaEditorStringId.Reorient);

    public AreaCameraControls() : this(AreaEditorTexts.English) { }

    public AreaCameraControls(AreaEditorTexts texts)
    {
        ArgumentNullException.ThrowIfNull(texts);
        _texts = texts;
        AvaloniaXamlLoader.Load(this);
        DataContext = this;
    }

    // The arrows move the camera, so the scene travels the other way - Aurora's left arrow sends
    // the scene right, its up arrow sends the scene down. Up and down travel forward and back
    // across the ground rather than changing altitude.
    private void OnPanLeft(object? sender, RoutedEventArgs e) => Viewport?.NudgePan(-1f, 0f);

    private void OnPanRight(object? sender, RoutedEventArgs e) => Viewport?.NudgePan(1f, 0f);

    private void OnPanUp(object? sender, RoutedEventArgs e) => Viewport?.NudgePan(0f, 1f);

    private void OnPanDown(object? sender, RoutedEventArgs e) => Viewport?.NudgePan(0f, -1f);

    private void OnOrbitLeft(object? sender, RoutedEventArgs e) => Viewport?.NudgeOrbit(-1f, 0f);

    private void OnOrbitRight(object? sender, RoutedEventArgs e) => Viewport?.NudgeOrbit(1f, 0f);

    private void OnOrbitUp(object? sender, RoutedEventArgs e) => Viewport?.NudgeOrbit(0f, 1f);

    private void OnOrbitDown(object? sender, RoutedEventArgs e) => Viewport?.NudgeOrbit(0f, -1f);

    private void OnZoomIn(object? sender, RoutedEventArgs e) => Viewport?.NudgeZoom(1);

    private void OnZoomOut(object? sender, RoutedEventArgs e) => Viewport?.NudgeZoom(-1);

    private void OnReorient(object? sender, RoutedEventArgs e) => Viewport?.ReorientCamera();

}
