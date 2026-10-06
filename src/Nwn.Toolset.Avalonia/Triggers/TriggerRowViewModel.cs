using Nwn.Authoring.Behaviors;
using Nwn.Toolset.Avalonia.Behaviors;

namespace Nwn.Toolset.Avalonia.Triggers;

/// <summary>
/// One row of the trigger editor: the shared behavior row plus the one thing only a transition has - a
/// live answer for its destination tag.
/// </summary>
public sealed class TriggerRowViewModel : BehaviorRowViewModel
{
    private readonly TransitionDestinationResolver? _resolveDestination;
    private readonly BehaviorEditorTexts _texts;

    /// <summary>The destination type has no default: an unset one is the thing the status reports.</summary>
    protected override bool SelectsFirstChoiceWhenUnset =>
        Definition.Name != TransitionDestinationFlags.LinkedToFlagsField;

    public TriggerRowViewModel(
        BehaviorFieldDefinition definition,
        BehaviorValueStore store,
        Func<string, Action, bool> runEdit,
        TransitionDestinationResolver? resolveDestination,
        BehaviorEditorTexts texts,
        IReadOnlyList<BehaviorChoice>? choices = null,
        Action? valueChanged = null,
        IBehaviorChoicePreviewProvider? previews = null)
        : base(definition, store, runEdit, choices, valueChanged, previews)
    {
        _resolveDestination = resolveDestination;
        _texts = texts ?? throw new ArgumentNullException(nameof(texts));
        Reload();
    }

    /// <summary>
    /// A destination row says what its tag reaches - the check that catches a transition pointing at a
    /// tag no door or waypoint carries, or at the wrong kind of object. A host with no resolver leaves
    /// the row without a status rather than reporting every tag missing.
    /// </summary>
    public override void RefreshStatus()
    {
        if (Definition.Kind != BehaviorFieldKind.TagReference || string.IsNullOrWhiteSpace(Text) ||
            _resolveDestination == null)
        {
            // No "required" here: the label already carries that badge, and printing it twice puts two
            // pieces of text in the same row from opposite ends.
            IsStatusGood = true;
            Status = null;
            return;
        }

        var scope = Definition.Name == TransitionDestinationFlags.LinkedToField
            ? TransitionDestinationFlags.ScopeOf(Store.GetInteger(
                BehaviorFieldStorage.Field, TransitionDestinationFlags.LinkedToFlagsField))
            : Definition.TagScope;

        var destination = TransitionDestinationDescriber.Resolve(scope, Text, _resolveDestination);
        Status = TransitionDestinationDescriber.Describe(destination, scope, _texts);
        IsStatusGood = destination.IsGood;
    }
}
