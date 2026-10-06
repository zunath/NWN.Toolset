namespace Nwn.Authoring.Behaviors;

/// <summary>What a transition's destination tag reaches, as far as a host can tell.</summary>
public enum TransitionDestinationStatus
{
    /// <summary>Exactly one object of the chosen type carries the tag.</summary>
    Resolved,

    /// <summary>Nothing carries the tag.</summary>
    NotFound,

    /// <summary>Several objects of the chosen type carry the tag, so the destination is not one place.</summary>
    Ambiguous,

    /// <summary>The tag is carried, but only by an object that is not of the chosen type.</summary>
    WrongType,

    /// <summary>
    /// Some of the module could not be read, so a missing or unique tag cannot be trusted either way.
    /// </summary>
    CatalogIncomplete,

    /// <summary>The destination has a tag but no door or waypoint type chosen for it.</summary>
    TypeUnset,

    /// <summary>The destination type is deliberately None, so the tag is not used.</summary>
    TypeNone,
}
