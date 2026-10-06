using Nwn.Authoring.Fields;

namespace Nwn.Toolset.Avalonia.Fields;

/// <summary>
/// A resref field backed by a list of what actually exists — the conversation picker on a
/// creature, door or placeable.
/// </summary>
/// <remarks>
/// Still a text box underneath, and deliberately: the field can legitimately name something the
/// module does not contain (a conversation from a hak, or one about to be created), so the list
/// is a convenience rather than a constraint. What it fixes is the ordinary case, where a
/// builder had to remember a sixteen-character resref exactly and got no warning when they did
/// not.
/// </remarks>
public partial class ResourcePickerFieldViewModel : TextFieldViewModel
{
    public ResourcePickerFieldViewModel(
        FieldDescriptor descriptor,
        EditorFieldContext context,
        IReadOnlyList<string> choices)
        : base(descriptor, context)
    {
        Choices = choices;
    }

    /// <summary>Everything of the right kind in the module, for the drop-down.</summary>
    public IReadOnlyList<string> Choices { get; }

    /// <summary>True when the current value names nothing in the module.</summary>
    public bool IsUnknown =>
        !string.IsNullOrWhiteSpace(Text)
        && !Choices.Contains(Text, StringComparer.OrdinalIgnoreCase);

    public string UnknownHint => IsUnknown
        ? Texts.Get(FieldStringId.UnknownResource)
        : string.Empty;

    protected override void OnTextCommitted()
    {
        OnPropertyChanged(nameof(IsUnknown));
        OnPropertyChanged(nameof(UnknownHint));
    }
}
