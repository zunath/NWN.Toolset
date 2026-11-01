using Nwn.Preview.Areas;

namespace Nwn.Toolset.Avalonia.Areas;

public sealed record AreaSelectionPick(
    IReadOnlyList<InstanceMarker> Instances,
    IReadOnlyList<(int Column, int Row)> Tiles,
    bool ExtendSelection);
