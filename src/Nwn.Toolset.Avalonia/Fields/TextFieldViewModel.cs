using CommunityToolkit.Mvvm.ComponentModel;
using Nwn.Authoring.Fields;

namespace Nwn.Toolset.Avalonia.Fields;

/// <summary>A single-line or multi-line text field.</summary>
public partial class TextFieldViewModel : FieldViewModel
{
    [ObservableProperty]
    private string _text = string.Empty;

    public TextFieldViewModel(FieldDescriptor descriptor, EditorFieldContext context)
        : base(descriptor, context)
    {
        RefreshFromDocument();
    }

    public sealed override void RefreshFromDocument()
    {
        Context.IsRefreshing = true;
        Text = SchemaFieldAccessor.GetText(Context.Document, Descriptor, Context.ResolveStrRef);
        Context.IsRefreshing = false;
        OnTextCommitted();
    }

    /// <summary>
    /// Called after the text settles, whether from an edit or a document refresh. Subclasses
    /// override it to re-evaluate anything derived from the value — a script slot uses it to
    /// re-check whether the script it names still exists.
    /// </summary>
    protected virtual void OnTextCommitted()
    {
    }

    partial void OnTextChanged(string value)
    {
        if (Context.IsRefreshing)
        {
            OnTextCommitted();
            return;
        }

        if (!Context.RunEdit(Texts.Get(FieldStringId.ChangeField, Label),
                () => SchemaFieldAccessor.SetText(Context.Document, Descriptor, value)))
            RefreshFromDocument();
        else
            OnTextCommitted();
    }
}
