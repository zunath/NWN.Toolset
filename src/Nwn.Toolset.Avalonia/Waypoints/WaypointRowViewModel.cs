using Nwn.Authoring.Behaviors;
using Nwn.Toolset.Avalonia.Behaviors;

namespace Nwn.Toolset.Avalonia.Waypoints;

/// <summary>
/// One row of the waypoint editor. The waypoint form needs nothing beyond the shared row's
/// shape, so this exists only to name the type its editor builds.
/// </summary>
public sealed class WaypointRowViewModel : BehaviorRowViewModel
{
    public WaypointRowViewModel(
        BehaviorFieldDefinition definition,
        BehaviorValueStore store,
        Func<string, Action, bool> runEdit,
        IReadOnlyList<BehaviorChoice>? choices = null,
        Action? valueChanged = null,
        IBehaviorChoicePreviewProvider? previews = null)
        : base(definition, store, runEdit, choices, valueChanged, previews)
    {
        Reload();
    }
}
