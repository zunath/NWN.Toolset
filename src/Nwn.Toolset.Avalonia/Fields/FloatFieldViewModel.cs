using CommunityToolkit.Mvvm.ComponentModel;
using Nwn.Authoring.Fields;

namespace Nwn.Toolset.Avalonia.Fields;

/// <summary>A float or double field.</summary>
public partial class FloatFieldViewModel : FieldViewModel
{
    [ObservableProperty]
    private double _value;

    public FloatFieldViewModel(FieldDescriptor descriptor, EditorFieldContext context)
        : base(descriptor, context)
    {
        RefreshFromDocument();
    }

    public sealed override void RefreshFromDocument()
    {
        Context.IsRefreshing = true;
        Value = SchemaFieldAccessor.GetFloat(Context.Document, Descriptor);
        Context.IsRefreshing = false;
    }

    partial void OnValueChanged(double value)
    {
        if (Context.IsRefreshing)
            return;

        if (!Context.RunEdit(Texts.Get(FieldStringId.ChangeField, Label),
                () => SchemaFieldAccessor.SetFloat(Context.Document, Descriptor, value)))
            RefreshFromDocument();
    }
}
