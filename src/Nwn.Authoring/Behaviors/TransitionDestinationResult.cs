namespace Nwn.Authoring.Behaviors;

/// <summary>
/// A host's answer to "what does this tag reach?": the status, plus the details each status needs to
/// be described.
/// </summary>
/// <param name="Status">What the tag reaches.</param>
/// <param name="Description">
/// For <see cref="TransitionDestinationStatus.Resolved"/>, where the destination is, in the host's own
/// words (for example "door in moseis_cantina"); empty otherwise.
/// </param>
/// <param name="MatchCount">
/// For <see cref="TransitionDestinationStatus.Ambiguous"/>, how many objects of the chosen type carry
/// the tag.
/// </param>
/// <param name="FoundAs">
/// For <see cref="TransitionDestinationStatus.WrongType"/>, the kind of object that does carry the tag.
/// </param>
public sealed record TransitionDestinationResult(
    TransitionDestinationStatus Status,
    string Description = "",
    int MatchCount = 0,
    BehaviorTagScope FoundAs = BehaviorTagScope.None)
{
    /// <summary>The answer for a tag nothing carries.</summary>
    public static TransitionDestinationResult NotFound { get; } = new(TransitionDestinationStatus.NotFound);

    /// <summary>The answer when part of the module could not be read.</summary>
    public static TransitionDestinationResult CatalogIncomplete { get; } =
        new(TransitionDestinationStatus.CatalogIncomplete);

    /// <summary>The answer for a tag with no destination type chosen.</summary>
    public static TransitionDestinationResult TypeUnset { get; } = new(TransitionDestinationStatus.TypeUnset);

    /// <summary>The answer for a tag whose destination type is deliberately None.</summary>
    public static TransitionDestinationResult TypeNone { get; } = new(TransitionDestinationStatus.TypeNone);

    /// <summary>True when nothing about the destination needs a builder's attention.</summary>
    public bool IsGood => Status is TransitionDestinationStatus.Resolved or TransitionDestinationStatus.TypeNone;

    public static TransitionDestinationResult Resolved(string description) =>
        new(TransitionDestinationStatus.Resolved, description);

    public static TransitionDestinationResult Ambiguous(int matchCount) =>
        new(TransitionDestinationStatus.Ambiguous, MatchCount: matchCount);

    public static TransitionDestinationResult WrongType(BehaviorTagScope foundAs) =>
        new(TransitionDestinationStatus.WrongType, FoundAs: foundAs);

    /// <summary>
    /// The answer of a host that only knows where a tag is defined: resolved at
    /// <paramref name="location"/>, or not found when that is null.
    /// </summary>
    public static TransitionDestinationResult FromLocation(string? location) =>
        location == null ? NotFound : Resolved(location);
}
