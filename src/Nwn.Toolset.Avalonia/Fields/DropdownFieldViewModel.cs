using CommunityToolkit.Mvvm.ComponentModel;
using Nwn.Authoring.Fields;

namespace Nwn.Toolset.Avalonia.Fields;

/// <summary>
/// 2DA-backed dropdown. When its lookup is unavailable, the stored numeric value remains
/// visible but read-only so missing metadata cannot turn a constrained field into free input.
/// </summary>
public partial class DropdownFieldViewModel : FieldViewModel
{
    public IReadOnlyList<LookupOption> Options { get; private set; }
    public bool HasOptions => Options.Count > 0;
    public string LookupUnavailableMessage => Texts.Get(FieldStringId.LookupUnavailable);

    [ObservableProperty]
    private LookupOption? _selectedOption;

    [ObservableProperty]
    private long _rawValue;

    public DropdownFieldViewModel(
        FieldDescriptor descriptor, EditorFieldContext context, IReadOnlyList<LookupOption> options)
        : base(descriptor, context)
    {
        Options = WithUnsetOption(options);
        RefreshFromDocument();
    }

    /// <summary>Rebuilds a live dropdown after its 2DA/TLK-backed labels change.</summary>
    public void RefreshOptions(IReadOnlyList<LookupOption> options)
    {
        Options = WithUnsetOption(options);
        OnPropertyChanged(nameof(Options));
        OnPropertyChanged(nameof(HasOptions));
        RefreshFromDocument();
    }

    public sealed override void RefreshFromDocument()
    {
        Context.IsRefreshing = true;
        RawValue = SchemaFieldAccessor.GetInteger(Context.Document, Descriptor);
        SelectedOption = Options.FirstOrDefault(option => option.Id == RawValue);
        Context.IsRefreshing = false;
    }

    partial void OnSelectedOptionChanged(LookupOption? value)
    {
        if (Context.IsRefreshing || value == null)
            return;

        if (!Context.RunEdit(Texts.Get(FieldStringId.ChangeField, Label),
                () => SchemaFieldAccessor.SetInteger(Context.Document, Descriptor, value.Id)))
            RefreshFromDocument();
    }

    private IReadOnlyList<LookupOption> WithUnsetOption(IReadOnlyList<LookupOption> options)
    {
        var unset = FieldUnsetSentinel.For(Descriptor.FieldType);
        return options.Count == 0 || Descriptor.IsRequired || options.Any(option => option.Id == unset)
            ? options
            : new[] { new LookupOption(unset, Texts.Get(FieldStringId.NoneOption)) }.Concat(options).ToList();
    }
}
