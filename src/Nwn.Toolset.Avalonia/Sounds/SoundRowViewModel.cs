using Nwn.Authoring.Behaviors;
using Nwn.Authoring.Sounds;
using Nwn.Toolset.Avalonia.Behaviors;

namespace Nwn.Toolset.Avalonia.Sounds;

/// <summary>
/// One row of the ambient-sound editor: the shared behavior row plus the ordered Sounds list,
/// which is the only control no other editor has.
/// </summary>
public sealed class SoundRowViewModel : BehaviorRowViewModel
{
    public bool IsSoundList => Definition.Kind == BehaviorFieldKind.SoundList;

    public SoundListEditorViewModel? SoundList { get; }

    /// <summary>
    /// A sound's palette category is stored, not defaulted: an absent field means the blueprint
    /// has never been filed, and showing the first category would claim otherwise.
    /// </summary>
    protected override bool SelectsFirstChoiceWhenUnset => false;

    public override bool HasValue => IsSoundList
        ? SoundList is { HasValidCount: true }
        : base.HasValue;

    public SoundRowViewModel(
        BehaviorFieldDefinition definition,
        SoundBehaviorValueStore store,
        Func<string, Action, bool> runEdit,
        IReadOnlyList<BehaviorChoice> choices,
        IReadOnlyList<string> audioResources,
        Action changed,
        ISoundListPreview? preview = null)
        : base(definition, store, runEdit, choices, changed)
    {
        if (IsSoundList)
        {
            SoundList = new SoundListEditorViewModel(
                store, new SoundListSchema(SoundBehaviorValueStore.SoundsField, SoundBehaviorValueStore.SoundEntryField),
                audioResources, definition.MaxItems, runEdit, OnListChanged, preview: preview);
        }

        Reload();
    }

    protected override void ReadValue()
    {
        if (IsSoundList)
        {
            SoundList?.Reload();
            return;
        }

        base.ReadValue();
    }

    private void OnListChanged()
    {
        NotifyValueShapeChanged();
        OnApplied();
    }
}
