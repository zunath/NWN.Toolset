using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Toolset.Avalonia.Localization;

namespace Nwn.Toolset.Avalonia.Variables;

/// <summary>Edits native locals through the owning editor's undo transaction.</summary>
public partial class VarTableSectionViewModel : ObservableObject
{
    private readonly Func<string, Action, bool> _runEdit;
    private readonly VarTable _varTable;
    private readonly Func<string, bool> _include;
    private readonly Func<string, string?>? _valueHint;
    private readonly VarTableTexts _texts;
    public ObservableCollection<VarTableRow> Rows { get; } = new();
    public IReadOnlyList<string> KnownKeys { get; }
    public IReadOnlyList<string> TypeChoices { get; } = new[] { "int", "float", "string" };
    public string NameLabel => _texts.Get(VarTableStringId.Name);
    public string TypeLabel => _texts.Get(VarTableStringId.Type);
    public string ValueLabel => _texts.Get(VarTableStringId.Value);
    public string NameWatermark => _texts.Get(VarTableStringId.VariableName);
    public string SetLabel => _texts.Get(VarTableStringId.Set);
    public string RemoveLabel => _texts.Get(VarTableStringId.Remove);
    [ObservableProperty] private VarTableRow? _selectedRow;
    [ObservableProperty] private string _newName = string.Empty;
    [ObservableProperty] private string _newType = "int";
    [ObservableProperty] private string _newValue = string.Empty;
    [ObservableProperty] private string? _validationHint;

    public VarTableSectionViewModel(Func<string, Action, bool> runEdit, VarTable varTable,
        IEnumerable<string>? knownKeys = null, Func<string, string?>? valueHint = null,
        Func<string, bool>? include = null, VarTableTexts? texts = null)
    {
        _runEdit = runEdit; _varTable = varTable; _include = include ?? (_ => true);
        _valueHint = valueHint; _texts = texts ?? VarTableTexts.English;
        KnownKeys = (knownKeys ?? Array.Empty<string>()).ToArray();
        RefreshFromDocument();
    }

    public void RefreshFromDocument()
    {
        Rows.Clear();
        foreach (var entry in _varTable)
        {
            if (!_include(entry.Name)) continue;
            var (typeLabel, value) = entry.Type switch
            {
                VarTable.TypeInt => ("int", entry.IntValue?.ToString() ?? string.Empty),
                VarTable.TypeFloat => ("float", entry.FloatValue?.ToString() ?? string.Empty),
                VarTable.TypeString => ("string", entry.StringValue ?? string.Empty),
                _ => (_texts.Get(VarTableStringId.UnsupportedType, entry.Type), string.Empty),
            };
            Rows.Add(new(entry.Name, typeLabel, value));
        }
    }

    [RelayCommand]
    private void SetVariable()
    {
        ValidationHint = null;
        var name = NewName.Trim();
        if (name.Length == 0) { ValidationHint = _texts.Get(VarTableStringId.NameRequired); return; }
        if (!_include(name)) { ValidationHint = _texts.Get(VarTableStringId.DedicatedEditor); return; }
        if (!CanEncodeNwnString(name) || (NewType == "string" && !CanEncodeNwnString(NewValue)))
        { ValidationHint = _texts.Get(VarTableStringId.InvalidEncoding); return; }
        var applied = NewType switch
        {
            "int" when int.TryParse(NewValue, out var intValue) => _runEdit(_texts.Get(VarTableStringId.SetVariable, name), () => _varTable.SetInt(name, intValue)),
            "float" when float.TryParse(NewValue, out var floatValue) && float.IsFinite(floatValue) => _runEdit(_texts.Get(VarTableStringId.SetVariable, name), () => _varTable.SetFloat(name, floatValue)),
            "string" => _runEdit(_texts.Get(VarTableStringId.SetVariable, name), () => _varTable.SetString(name, NewValue)),
            _ => false,
        };
        if (!applied)
        { ValidationHint = NewType == "string" ? _texts.Get(VarTableStringId.StringApplyFailed) : _texts.Get(VarTableStringId.InvalidValue, NewValue, NewType); return; }
        ValidationHint = _valueHint?.Invoke(name);
        RefreshFromDocument();
    }

    [RelayCommand]
    private void RemoveSelected()
    {
        if (SelectedRow is not { } row) return;
        if (_runEdit(_texts.Get(VarTableStringId.RemoveVariable, row.Name), () => _varTable.Remove(row.Name))) RefreshFromDocument();
    }

    partial void OnSelectedRowChanged(VarTableRow? value)
    {
        if (value is null) return;
        NewName = value.Name; NewType = value.TypeLabel is "int" or "float" or "string" ? value.TypeLabel : "int"; NewValue = value.Value;
    }

    private static bool CanEncodeNwnString(string value)
    {
        try { JsonStringCodec.Encode(value); return true; }
        catch (EncoderFallbackException) { return false; }
    }
}
