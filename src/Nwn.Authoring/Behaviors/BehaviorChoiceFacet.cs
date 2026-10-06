namespace Nwn.Authoring.Behaviors
{
    /// <summary>
    /// One reusable filter value carried by a visual choice. The shared gallery discovers these
    /// facets rather than knowing which editor or resource type supplied them.
    /// </summary>
    public sealed record BehaviorChoiceFacet(
        string GroupKey,
        string GroupLabel,
        string ValueKey,
        string Display,
        int Order = 0);
}
