using CommunityToolkit.Mvvm.Input;
using Nwn.Authoring.Fields;

namespace Nwn.Toolset.Avalonia.Fields;

/// <summary>
/// A script slot: resref text, plus the ability to browse, open and create the script it names.
/// </summary>
/// <remarks>
/// The missing-script warning is the point. A slot pointing at a script that does not exist is a
/// live and otherwise invisible class of bug — thousands of module resources name a script by
/// resref, nothing validates them, and the failure only shows up in-game as an event that silently
/// does nothing.
/// </remarks>
public partial class ScriptFieldViewModel : TextFieldViewModel
{
    private readonly IScriptSlotHost? _host;

    public ScriptFieldViewModel(FieldDescriptor descriptor, EditorFieldContext context, IScriptSlotHost? host = null)
        : base(descriptor, context)
    {
        _host = host;
    }

    public bool CanBrowse => _host != null;

    /// <summary>True when this slot names a script that is not in the module.</summary>
    public bool IsMissing =>
        _host != null && !string.IsNullOrWhiteSpace(Text) && !_host.ScriptExists(Text);

    public string MissingMessage => Texts.Get(FieldStringId.MissingScript, Text);

    [RelayCommand]
    private async Task Browse()
    {
        if (_host == null)
            return;

        var chosen = await _host.PickScriptAsync(Text).ConfigureAwait(true);
        if (chosen != null)
            Text = chosen;
    }

    [RelayCommand]
    private void OpenScript()
    {
        if (!string.IsNullOrWhiteSpace(Text))
            _host?.OpenScript(Text);
    }

    protected override void OnTextCommitted() => RaiseMissing();

    private void RaiseMissing()
    {
        OnPropertyChanged(nameof(IsMissing));
        OnPropertyChanged(nameof(MissingMessage));
    }
}
