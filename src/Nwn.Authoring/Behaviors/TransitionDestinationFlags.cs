namespace Nwn.Authoring.Behaviors;

/// <summary>The native <c>LinkedTo</c> and <c>LinkedToFlags</c> fields of a transition's destination.</summary>
public static class TransitionDestinationFlags
{
    /// <summary>The GFF field naming the destination tag.</summary>
    public const string LinkedToField = "LinkedTo";

    /// <summary>The GFF field naming the destination type.</summary>
    public const string LinkedToFlagsField = "LinkedToFlags";

    /// <summary>The stored value of a destination that is a door.</summary>
    public const long Door = 1;

    /// <summary>The stored value of a destination that is a waypoint.</summary>
    public const long Waypoint = 2;

    /// <summary>The kind of object a stored <c>LinkedToFlags</c> value names, or None for any other value.</summary>
    public static BehaviorTagScope ScopeOf(long? flags) => flags switch
    {
        Door => BehaviorTagScope.Door,
        Waypoint => BehaviorTagScope.Waypoint,
        _ => BehaviorTagScope.None,
    };
}
