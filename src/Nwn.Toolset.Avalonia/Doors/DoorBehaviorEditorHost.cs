using Nwn.Authoring.Behaviors;
using Nwn.Authoring.Doors;
using Nwn.Toolset.Avalonia.Behaviors;

namespace Nwn.Toolset.Avalonia.Doors;

/// <summary>The game data and services a door behavior editor takes from its host.</summary>
public sealed class DoorBehaviorEditorHost : BehaviorEditorHost
{
    /// <summary>The host's door behaviors, Basic rows and door conventions.</summary>
    public required IDoorBehaviorCatalog Catalog { get; init; }

    /// <summary>
    /// Says what a tag reference reaches: found, missing, ambiguous, of the wrong type, unverifiable or
    /// without a destination type. Rows report a missing tag when null.
    /// </summary>
    public TransitionDestinationResolver? ResolveDestination { get; init; }

    /// <summary>The combined generic and specific door appearances.</summary>
    public IReadOnlyList<DoorAppearanceChoice> Appearances { get; init; } = Array.Empty<DoorAppearanceChoice>();

    /// <summary>Renders the appearance gallery's thumbnails; no pictures when null.</summary>
    public IDoorAppearancePreviewSource? AppearancePreviews { get; init; }

    /// <summary>Every key item a key-item sequence row can require, by id.</summary>
    public IReadOnlyDictionary<int, string>? KeyItems { get; init; }
}
