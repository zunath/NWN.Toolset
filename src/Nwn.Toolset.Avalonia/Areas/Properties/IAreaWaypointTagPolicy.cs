using Nwn.Authoring.Documents.NimGff;

namespace Nwn.Toolset.Avalonia.Areas.Properties;

/// <summary>
/// The host's rule for destination tags only one placed waypoint in the module may carry, so the
/// waypoint section can refuse a save that would make a destination ambiguous.
/// </summary>
public interface IAreaWaypointTagPolicy
{
    /// <summary>True when only one placed waypoint in the module may carry <paramref name="tag"/>.</summary>
    bool IsSingletonTag(string tag);

    /// <summary>The tag a placed waypoint carries as the host's tag index resolves it.</summary>
    string? ResolveTag(JsonGffStruct waypoint);

    /// <summary>Placed waypoints carrying <paramref name="tag"/> in the module's other areas.</summary>
    int CountPlacementsOutsideArea(string tag);
}
