using Nwn.Authoring.Editing;
using Nwn.Toolset.Avalonia.Palettes.Workflow;

namespace Nwn.Toolset.Avalonia.Areas.Properties;

/// <summary>What one placed-instance section takes from its area editor.</summary>
public sealed class AreaInstanceSectionHost
{
    /// <summary>The area's GIT session; the section edits one of its instance lists.</summary>
    public required DocumentSession Instances { get; init; }

    /// <summary>The area's GIC session, whose comment lists stay index-aligned with the GIT.</summary>
    public required DocumentSession Comments { get; init; }

    /// <summary>
    /// The host transaction every instance edit runs through - normally its
    /// <c>AreaDocumentEditSession.ExecuteInstances</c> with its own history and refresh bookkeeping.
    /// Returns false when the edit was refused.
    /// </summary>
    public required Func<string, Action, bool> RunEdit { get; init; }

    /// <summary>The blueprints new placements are created from.</summary>
    public required IAreaInstanceBlueprintSource Blueprints { get; init; }

    /// <summary>The palette the Add flow browses; Add does nothing when null.</summary>
    public IAreaInstancePaletteSource? Palettes { get; init; }

    /// <summary>Resolves palette entries that name themselves by StrRef.</summary>
    public Func<uint, string?>? ResolveStrRef { get; init; }

    /// <summary>Builds typed editors and local-variable editors for selected placements.</summary>
    public IAreaInstanceEditorFactory? Editors { get; init; }

    /// <summary>The singleton destination-tag rule for the waypoint section.</summary>
    public IAreaWaypointTagPolicy? WaypointTags { get; init; }

    /// <summary>Receives failures to read palettes or create placements.</summary>
    public IPaletteLog? Log { get; init; }

    /// <summary>The page's captions and edit descriptions.</summary>
    public AreaPropertiesTexts Texts { get; init; } = AreaPropertiesTexts.English;
}
