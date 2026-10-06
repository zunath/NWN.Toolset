using Nwn.Authoring.Areas.Placement;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Toolset.Avalonia.Areas.Properties;

namespace Nwn.Toolset.Avalonia.Tests.Support;

internal sealed class FakeAreaWaypointTagPolicy : IAreaWaypointTagPolicy
{
    public HashSet<string> Singletons { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, int> OutsideArea { get; } = new(StringComparer.OrdinalIgnoreCase);

    public bool IsSingletonTag(string tag) => Singletons.Contains(tag);

    public string? ResolveTag(JsonGffStruct waypoint) => InstanceFieldMap.GetTag(waypoint);

    public int CountPlacementsOutsideArea(string tag) => OutsideArea.TryGetValue(tag, out var count) ? count : 0;
}
