using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Toolset.Avalonia.Doors;
using Nwn.Toolset.Avalonia.Sounds;
using Nwn.Toolset.Avalonia.Triggers;
using Nwn.Toolset.Avalonia.Variables;
using Nwn.Toolset.Avalonia.Waypoints;

namespace Nwn.Toolset.Avalonia.Areas.Properties;

/// <summary>
/// Builds the shared typed editors from host data. A host with its own editor subclasses
/// implements <see cref="IAreaInstanceEditorFactory"/> instead.
/// </summary>
/// <param name="headerOwner">The file the placements live in, shown in each editor's header.</param>
/// <param name="doors">Door data; doors keep the generic form when null.</param>
/// <param name="waypoints">Waypoint data; waypoints keep the generic form when null.</param>
/// <param name="sounds">Sound data; sounds keep the generic form when null.</param>
/// <param name="triggers">Trigger data; triggers keep the generic form when null.</param>
/// <param name="variables">Builds local-variable editors; the plain editor when null.</param>
public sealed class AreaInstanceEditorFactory(
    string headerOwner,
    DoorBehaviorEditorHost? doors = null,
    WaypointBehaviorEditorHost? waypoints = null,
    SoundBehaviorEditorHost? sounds = null,
    TriggerBehaviorEditorHost? triggers = null,
    IVarTableSectionFactory? variables = null) : IAreaInstanceEditorFactory
{
    public DoorBehaviorEditorViewModel? CreateDoor(
        JsonGffStruct door, Func<string, Action, bool> runEdit, bool isDocumentDirty) =>
        doors == null
            ? null
            : new DoorBehaviorEditorViewModel(door, headerOwner, isInstance: true, runEdit, doors, isDocumentDirty);

    public WaypointBehaviorEditorViewModel? CreateWaypoint(
        JsonGffStruct waypoint, Func<string, Action, bool> runEdit, Func<string, bool> singletonTagInUse) =>
        waypoints == null
            ? null
            : new WaypointBehaviorEditorViewModel(
                waypoint, headerOwner, isInstance: true, runEdit, waypoints, singletonTagInUse);

    public SoundBehaviorEditorViewModel? CreateSound(JsonGffStruct sound, Func<string, Action, bool> runEdit) =>
        sounds == null
            ? null
            : new SoundBehaviorEditorViewModel(sound, headerOwner, isInstance: true, runEdit, sounds);

    public TriggerBehaviorEditorViewModel? CreateTrigger(JsonGffStruct trigger, Func<string, Action, bool> runEdit) =>
        triggers == null
            ? null
            : new TriggerBehaviorEditorViewModel(trigger, headerOwner, isInstance: true, runEdit, triggers);

    public VarTableSectionViewModel CreateVariables(Func<string, Action, bool> runEdit, VarTable table) =>
        variables?.Create(runEdit, table) ?? new VarTableSectionViewModel(runEdit, table);
}
