using CommunityToolkit.Mvvm.ComponentModel;
using Nwn.Authoring.Fields;

namespace Nwn.Toolset.Avalonia.Fields;

/// <summary>A byte 0/1 field shown as a checkbox.</summary>
public partial class CheckFieldViewModel : FieldViewModel
{
    [ObservableProperty]
    private bool _isChecked;

    public CheckFieldViewModel(FieldDescriptor descriptor, EditorFieldContext context)
        : base(descriptor, context)
    {
        RefreshFromDocument();
    }

    public sealed override void RefreshFromDocument()
    {
        Context.IsRefreshing = true;
        IsChecked = SchemaFieldAccessor.GetBool(Context.Document, Descriptor);
        Context.IsRefreshing = false;
    }

    partial void OnIsCheckedChanged(bool value)
    {
        if (Context.IsRefreshing)
            return;

        if (!Context.RunEdit(Texts.Get(FieldStringId.ToggleField, Label),
                () => SchemaFieldAccessor.SetBool(Context.Document, Descriptor, value)))
            RefreshFromDocument();
    }
}
