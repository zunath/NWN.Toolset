using CommunityToolkit.Mvvm.ComponentModel;
using Nwn.Authoring.Fields;

namespace Nwn.Toolset.Avalonia.Fields;

/// <summary>Base of all per-field view models; subclass per EditorKind for clean templates.</summary>
public abstract partial class FieldViewModel : ObservableObject
{
    protected readonly EditorFieldContext Context;
    public FieldDescriptor Descriptor { get; }

    public string Label => Descriptor.Label;
    public string? Description => Descriptor.Description;
    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);
    public bool IsReadOnly => Descriptor.IsReadOnly;
    public bool IsMultiline => Descriptor.IsMultiline;
    public bool IsSingleLine => !Descriptor.IsMultiline;

    /// <summary>The captions and messages this field shows.</summary>
    public FieldTexts Texts => Context.Texts;

    protected FieldViewModel(FieldDescriptor descriptor, EditorFieldContext context)
    {
        Descriptor = descriptor;
        Context = context;
    }

    /// <summary>Reloads the view model's value from the document (after undo/redo).</summary>
    public abstract void RefreshFromDocument();
}
