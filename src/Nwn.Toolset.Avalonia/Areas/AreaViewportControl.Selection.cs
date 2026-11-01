using System.Numerics;
using Avalonia;
using Nwn.Preview.Areas;

namespace Nwn.Toolset.Avalonia.Areas;

public partial class AreaViewportControl
{
    private IReadOnlyList<InstanceMarker> _selectedInstances = Array.Empty<InstanceMarker>();
    private IReadOnlyList<(int Column, int Row)> _selectedTileCells = Array.Empty<(int, int)>();
    public IReadOnlyList<InstanceMarker> SelectedInstances
    {
        get => _selectedInstances;
        set { _selectedInstances = value; RequestNextFrameRendering(); }
    }
    public IReadOnlyList<(int Column, int Row)> SelectedTileCells
    {
        get => _selectedTileCells;
        set { _selectedTileCells = value; RequestNextFrameRendering(); }
    }
    public event Action<AreaSelectionPick>? SelectionPicked;

    private void RaiseAdditiveSelection(Point position)
    {
        if (Scene is not { } scene || TryBuildRay(position) is not { } ray)
            return;
        var hit = AreaPicking.PickClosestInstance(ray, scene, showPlaceableModels: true);
        var cell = hit == null ? ResolveTileCell(ray, scene) : null;
        SelectionPicked?.Invoke(new AreaSelectionPick(hit == null ? Array.Empty<InstanceMarker>() : new[] { hit },
            cell is { } tile ? new[] { tile } : Array.Empty<(int, int)>(), true));
    }

    private void RaiseRectangleSelection(Point end, bool extend)
    {
        if (Scene is not { } scene || _viewportWidth <= 0 || _viewportHeight <= 0)
            return;
        var box = new Rect(new Point(Math.Min(_pressStartPos.X, end.X), Math.Min(_pressStartPos.Y, end.Y)),
            new Point(Math.Max(_pressStartPos.X, end.X), Math.Max(_pressStartPos.Y, end.Y)));
        var instances = scene.Instances.Where(marker => IsInside(marker.Position)).ToArray();
        var tiles = instances.Length > 0 ? Array.Empty<(int, int)>() : scene.Tiles
            .Where(tile => IsInside(new Vector3(tile.CenterX, tile.CenterY, tile.HeightOffset)))
            .Select(tile => (tile.Column, tile.Row)).ToArray();
        SelectionPicked?.Invoke(new AreaSelectionPick(instances, tiles, extend));

        bool IsInside(Vector3 point)
        {
            var clip = Vector4.Transform(new Vector4(point, 1), _lastView * _lastProjection);
            return clip.W > 0 && box.Contains(new Point(
                (clip.X / clip.W + 1) * _viewportWidth / 2,
                (1 - clip.Y / clip.W) * _viewportHeight / 2));
        }
    }
}
