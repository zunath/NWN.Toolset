namespace Nwn.Authoring.Behaviors;

/// <summary>
/// Answers what a tag reaches among the module's objects of one kind.
/// </summary>
/// <param name="scope">
/// The kind of object the tag must name. <see cref="BehaviorTagScope.None"/> asks about a tag whose
/// destination type is not a door or a waypoint, which the host answers with
/// <see cref="TransitionDestinationStatus.TypeUnset"/> or <see cref="TransitionDestinationStatus.TypeNone"/>
/// according to whether it treats that type as unset or as a deliberate "none".
/// </param>
/// <param name="tag">The tag, never blank.</param>
public delegate TransitionDestinationResult TransitionDestinationResolver(BehaviorTagScope scope, string tag);
