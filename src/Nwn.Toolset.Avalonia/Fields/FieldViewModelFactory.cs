using Nwn.Authoring.Fields;

namespace Nwn.Toolset.Avalonia.Fields;

/// <summary>
/// Builds the field view model a <see cref="FieldDescriptor"/>'s <see cref="EditorKind"/> calls
/// for. Shared by every schema-driven editor so a new kind is wired in exactly one place.
/// </summary>
public static class FieldViewModelFactory
{
    /// <param name="descriptor">The field to edit.</param>
    /// <param name="context">The document and transaction the field edits through.</param>
    /// <param name="lookupOptions">
    /// Resolves the options of a 2DA-backed dropdown by its lookup key. An empty list degrades the
    /// field to a read-only number, which keeps the stored value visible.
    /// </param>
    /// <param name="scriptSlotHost">
    /// Lets a script slot browse, open and validate the script it names. Null leaves the slot as
    /// plain resref text.
    /// </param>
    /// <param name="resourceChoices">
    /// Resolves the module resources of one kind, keyed by extension, for a resref field that
    /// names another resource — a creature's conversation, say. Null leaves it free text.
    /// </param>
    public static FieldViewModel Create(
        FieldDescriptor descriptor,
        EditorFieldContext context,
        Func<string?, IReadOnlyList<LookupOption>>? lookupOptions = null,
        IScriptSlotHost? scriptSlotHost = null,
        Func<string?, IReadOnlyList<string>>? resourceChoices = null)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(context);

        return descriptor.Kind switch
        {
            EditorKind.Integer => new IntegerFieldViewModel(descriptor, context),
            EditorKind.Float => new FloatFieldViewModel(descriptor, context),
            EditorKind.Check => new CheckFieldViewModel(descriptor, context),
            EditorKind.LocString => new LocStringFieldViewModel(descriptor, context),
            EditorKind.TwoDaDropdown => new DropdownFieldViewModel(
                descriptor, context, lookupOptions?.Invoke(descriptor.LookupKey) ?? Array.Empty<LookupOption>()),
            EditorKind.ScriptSlot => new ScriptFieldViewModel(descriptor, context, scriptSlotHost),
            EditorKind.ResourcePicker => new ResourcePickerFieldViewModel(
                descriptor, context,
                resourceChoices?.Invoke(descriptor.LookupKey) ?? Array.Empty<string>()),
            _ => new TextFieldViewModel(descriptor, context)
        };
    }
}
