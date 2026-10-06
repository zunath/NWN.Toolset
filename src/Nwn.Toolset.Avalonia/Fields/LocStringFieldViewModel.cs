using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Fields;

namespace Nwn.Toolset.Avalonia.Fields;

/// <summary>LocString fields edit the language-0 text; the strref (if any) is displayed.</summary>
public partial class LocStringFieldViewModel : FieldViewModel
{
    [ObservableProperty]
    private string _text = string.Empty;

    public string? StrRefDisplay { get; private set; }
    public uint? StrRef { get; private set; }
    public bool CanOpenTlkRow => StrRef is { } strRef && Context.CanOpenTlkRow(strRef);

    public LocStringFieldViewModel(FieldDescriptor descriptor, EditorFieldContext context)
        : base(descriptor, context)
    {
        RefreshFromDocument();
    }

    public sealed override void RefreshFromDocument()
    {
        Context.IsRefreshing = true;

        // Deliberately the override only, not the resolved strref. This box edits the language-0
        // text, so showing TLK text in it would invite a stray keystroke to promote a strref-backed
        // name into a literal override of the same words - a silent change to what the field means.
        // The TLK text belongs beside the strref instead, where it explains the blank rather than
        // pretending to be it.
        Text = SchemaFieldAccessor.GetText(Context.Document, Descriptor);

        var field = Context.Document.Root.GetOrNull(Descriptor.FieldName);
        if (field?.GetLocStringId() is { } id)
        {
            StrRef = id;
            var resolved = Context.ResolveStrRef?.Invoke(id);
            StrRefDisplay = string.IsNullOrWhiteSpace(resolved)
                ? Texts.Get(FieldStringId.StrRef, id)
                : Texts.Get(FieldStringId.StrRefResolved, id, resolved);
        }
        else
        {
            StrRef = null;
            StrRefDisplay = null;
        }

        OnPropertyChanged(nameof(StrRefDisplay));
        OnPropertyChanged(nameof(StrRef));
        OnPropertyChanged(nameof(CanOpenTlkRow));
        OpenTlkRowCommand.NotifyCanExecuteChanged();
        Context.IsRefreshing = false;
    }

    [RelayCommand(CanExecute = nameof(CanOpenTlkRow))]
    private void OpenTlkRow()
    {
        if (StrRef.HasValue)
            Context.OpenTlkRow?.Invoke(StrRef.Value);
    }

    partial void OnTextChanged(string value)
    {
        if (Context.IsRefreshing)
            return;

        if (!Context.RunEdit(Texts.Get(FieldStringId.ChangeField, Label),
                () => SchemaFieldAccessor.SetText(Context.Document, Descriptor, value)))
            RefreshFromDocument();
    }
}
