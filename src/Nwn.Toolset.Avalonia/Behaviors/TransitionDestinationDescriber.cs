using Nwn.Authoring.Behaviors;

namespace Nwn.Toolset.Avalonia.Behaviors;

/// <summary>
/// Turns a host's answer about a tag into the status line a door or trigger destination row prints.
/// </summary>
public static class TransitionDestinationDescriber
{
    /// <summary>
    /// What a tag reaches: the host's answer, or - when the host has no resolver - an unset type for a
    /// destination with no kind and a missing tag for every other.
    /// </summary>
    public static TransitionDestinationResult Resolve(
        BehaviorTagScope scope, string tag, TransitionDestinationResolver? resolver)
    {
        ArgumentNullException.ThrowIfNull(tag);
        if (resolver != null)
            return resolver(scope, tag);

        return scope == BehaviorTagScope.None
            ? TransitionDestinationResult.TypeUnset
            : TransitionDestinationResult.NotFound;
    }

    /// <summary>The line a row prints for <paramref name="result"/>, a tag expected to name a <paramref name="scope"/>.</summary>
    public static string Describe(
        TransitionDestinationResult result, BehaviorTagScope scope, BehaviorEditorTexts texts)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(texts);

        return result.Status switch
        {
            TransitionDestinationStatus.Resolved =>
                texts.Get(BehaviorEditorStringId.TagResolved, result.Description),
            TransitionDestinationStatus.NotFound => texts.Get(scope switch
            {
                BehaviorTagScope.Item => BehaviorEditorStringId.NoItemTag,
                BehaviorTagScope.Waypoint => BehaviorEditorStringId.NoWaypointTag,
                BehaviorTagScope.Door => BehaviorEditorStringId.NoDoorTag,
                _ => BehaviorEditorStringId.NoDestinationTag,
            }),
            TransitionDestinationStatus.Ambiguous => texts.Get(
                BehaviorEditorStringId.DestinationAmbiguous, result.MatchCount, KindName(scope, texts)),
            TransitionDestinationStatus.WrongType => texts.Get(
                BehaviorEditorStringId.DestinationWrongType,
                KindName(result.FoundAs, texts),
                KindName(scope, texts)),
            TransitionDestinationStatus.CatalogIncomplete =>
                texts.Get(BehaviorEditorStringId.DestinationCatalogIncomplete),
            TransitionDestinationStatus.TypeUnset => texts.Get(BehaviorEditorStringId.DestinationTypeUnset),
            TransitionDestinationStatus.TypeNone => texts.Get(BehaviorEditorStringId.DestinationTypeNone),
            _ => throw new ArgumentOutOfRangeException(nameof(result), result.Status, null),
        };
    }

    private static string KindName(BehaviorTagScope scope, BehaviorEditorTexts texts) => texts.Get(scope switch
    {
        BehaviorTagScope.Door => BehaviorEditorStringId.DestinationKindDoor,
        BehaviorTagScope.Waypoint => BehaviorEditorStringId.DestinationKindWaypoint,
        BehaviorTagScope.Item => BehaviorEditorStringId.DestinationKindItem,
        _ => BehaviorEditorStringId.DestinationKindDoorOrWaypoint,
    });
}
