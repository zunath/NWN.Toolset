using CommunityToolkit.Mvvm.ComponentModel;
using Nwn.Authoring.Fields;

namespace Nwn.Toolset.Avalonia.Fields;

/// <summary>An integer field of any native width.</summary>
public partial class IntegerFieldViewModel : FieldViewModel
{
    [ObservableProperty]
    private long _value;

    public IntegerFieldViewModel(FieldDescriptor descriptor, EditorFieldContext context)
        : base(descriptor, context)
    {
        RefreshFromDocument();
    }

    public sealed override void RefreshFromDocument()
    {
        Context.IsRefreshing = true;
        Value = SchemaFieldAccessor.GetInteger(Context.Document, Descriptor);
        Context.IsRefreshing = false;
    }

    partial void OnValueChanged(long value)
    {
        if (Context.IsRefreshing)
            return;

        if (!Context.RunEdit(Texts.Get(FieldStringId.ChangeField, Label),
                () => SchemaFieldAccessor.SetInteger(Context.Document, Descriptor, value)))
            RefreshFromDocument();
    }
}
