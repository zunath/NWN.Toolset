using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nwn.Authoring.Documents.Native;
using Nwn.Toolset.Avalonia.Palettes.Workflow;

namespace Nwn.Toolset.Avalonia.Areas.Properties;

/// <summary>
/// Browses one palette tree for a blueprint type and lets the caller pick a leaf's resref
/// to place (<c>onResRefChosen</c>), or cancel (<c>onCancelled</c>) - both close the host's popup.
/// </summary>
/// <remarks>
/// Strictly read-only. Categories are organized in the Palette panel, whose authority is the
/// toolset's own category sidecar; this browser reads the module's legacy palette only because
/// it is what an area section's Add flow has to offer. Editing it here would write an
/// arrangement the Palette panel never reads and the game or Aurora may later overwrite.
/// </remarks>
public partial class PaletteBrowserViewModel : ObservableObject
{
    private readonly AreaPropertiesTexts _texts;
    private Action<string> _onResRefChosen;
    private Action _onCancelled;

    public string Title { get; }

    /// <summary>The browser's heading, naming the kind of blueprint it places.</summary>
    public string Heading => _texts.Get(AreaPropertiesStringId.BrowserHeading, Title);

    public string PlaceLabel => _texts.Get(AreaPropertiesStringId.BrowserPlace);

    public string CancelLabel => _texts.Get(AreaPropertiesStringId.BrowserCancel);

    public ObservableCollection<PaletteNodeViewModel> Nodes { get; } = new();

    [ObservableProperty]
    private PaletteNodeViewModel? _selectedNode;

    [ObservableProperty]
    private string? _statusMessage;

    /// <param name="title">The section's blueprint kind, as in "Creatures".</param>
    /// <param name="source">The palette's location, named in failure messages.</param>
    /// <param name="readNodes">Reads the palette's root nodes; a failure leaves the browser empty.</param>
    /// <param name="onResRefChosen">Completes with the chosen leaf's resref.</param>
    /// <param name="onCancelled">Completes when the browser is dismissed.</param>
    /// <param name="log">Receives read failures.</param>
    /// <param name="resolveStrRef">Resolves palette entries that name themselves by StrRef.</param>
    /// <param name="texts">The browser's captions and messages.</param>
    public PaletteBrowserViewModel(
        string title,
        string source,
        Func<IReadOnlyList<PaletteNode>> readNodes,
        Action<string> onResRefChosen,
        Action onCancelled,
        IPaletteLog? log,
        Func<uint, string?>? resolveStrRef = null,
        AreaPropertiesTexts? texts = null)
    {
        ArgumentNullException.ThrowIfNull(readNodes);
        Title = title;
        _texts = texts ?? AreaPropertiesTexts.English;
        _onResRefChosen = onResRefChosen ?? throw new ArgumentNullException(nameof(onResRefChosen));
        _onCancelled = onCancelled ?? throw new ArgumentNullException(nameof(onCancelled));

        try
        {
            foreach (var node in readNodes().Where(n => n.DeleteMe != true))
                Nodes.Add(new PaletteNodeViewModel(node, resolveStrRef, _texts));
        }
        catch (Exception ex)
        {
            log?.Write(_texts.Get(AreaPropertiesStringId.PaletteReadFailed, source, ex.Message));
            StatusMessage = _texts.Get(AreaPropertiesStringId.PaletteUnreadable, ex.Message);
        }
    }

    /// <summary>
    /// Updates what choosing or cancelling this already-open browser completes. The same browser
    /// can move between the Properties Add flow and the scene Place flow while retaining its selection.
    /// </summary>
    public void RebindCompletionActions(Action<string> onResRefChosen, Action onCancelled)
    {
        _onResRefChosen = onResRefChosen ?? throw new ArgumentNullException(nameof(onResRefChosen));
        _onCancelled = onCancelled ?? throw new ArgumentNullException(nameof(onCancelled));
    }

    partial void OnSelectedNodeChanged(PaletteNodeViewModel? value) => StatusMessage = null;

    [RelayCommand]
    private void Choose()
    {
        if (SelectedNode is { IsLeaf: true, ResRef: { } resRef })
            _onResRefChosen(resRef);
        else
            StatusMessage = _texts.Get(AreaPropertiesStringId.SelectLeaf);
    }

    [RelayCommand]
    private void Cancel() => _onCancelled();
}
