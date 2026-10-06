using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Toolset.Avalonia.Doors;
using Nwn.Toolset.Avalonia.Sounds;
using Nwn.Toolset.Avalonia.Triggers;
using Nwn.Toolset.Avalonia.Variables;
using Nwn.Toolset.Avalonia.Waypoints;

namespace Nwn.Toolset.Avalonia.Areas.Properties;

/// <summary>
/// Builds the typed editor shown under a selected placement. A null editor leaves that kind on the
/// generic tag/position form with its local variables.
/// </summary>
public interface IAreaInstanceEditorFactory
{
    DoorBehaviorEditorViewModel? CreateDoor(JsonGffStruct door, Func<string, Action, bool> runEdit, bool isDocumentDirty);

    /// <param name="waypoint">The placed waypoint.</param>
    /// <param name="runEdit">The section transaction every write runs through.</param>
    /// <param name="singletonTagInUse">True when another placed waypoint already carries a singleton tag.</param>
    WaypointBehaviorEditorViewModel? CreateWaypoint(
        JsonGffStruct waypoint, Func<string, Action, bool> runEdit, Func<string, bool> singletonTagInUse);

    SoundBehaviorEditorViewModel? CreateSound(JsonGffStruct sound, Func<string, Action, bool> runEdit);

    /// <summary>
    /// The typed editor of a placed trigger, whose destination, trap and script fields it edits beside the
    /// area's own transform and geometry form; null keeps the generic form.
    /// </summary>
    TriggerBehaviorEditorViewModel? CreateTrigger(JsonGffStruct trigger, Func<string, Action, bool> runEdit);

    /// <summary>The local-variable editor of a placement without a typed editor.</summary>
    VarTableSectionViewModel CreateVariables(Func<string, Action, bool> runEdit, VarTable table);
}
