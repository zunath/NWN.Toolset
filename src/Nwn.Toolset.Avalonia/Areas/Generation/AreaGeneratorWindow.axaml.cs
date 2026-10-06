using Avalonia.Controls;
using Avalonia.Interactivity;
using Nwn.Toolset.Avalonia.Localization;

namespace Nwn.Toolset.Avalonia.Areas.Generation;

/// <summary>The Area Generator window: previews a deterministic layout and creates it as a new area.</summary>
public partial class AreaGeneratorWindow : Window
{
    private const string OptionLabelsResourceKey = "OptionLabels";

    public AreaGeneratorWindow()
        : this(AreaGeneratorTexts.English, null)
    {
    }

    public AreaGeneratorWindow(AreaGeneratorViewModel viewModel)
        : this(viewModel?.Texts ?? throw new ArgumentNullException(nameof(viewModel)), null)
    {
        DataContext = viewModel;
        Closing += OnClosing;
        viewModel.AreaCreated += resref => Close(resref);
    }

    /// <summary>Creates a window whose title and icon come from the host's options.</summary>
    public AreaGeneratorWindow(AreaGeneratorViewModel viewModel, AreaGeneratorWindowOptions? options)
        : this(viewModel)
    {
        ApplyOptions(viewModel.Texts, options);
    }

    private AreaGeneratorWindow(AreaGeneratorTexts texts, AreaGeneratorWindowOptions? options)
    {
        Resources[OptionLabelsResourceKey] = new AreaGeneratorOptionLabelConverter(texts);
        InitializeComponent();
        ApplyOptions(texts, options);
        Opened += OnOpened;
        Closed += OnClosed;
    }

    /// <summary>Shows the generator over the owner and returns the created area's ResRef, or null when none was created.</summary>
    public static async Task<string?> ShowAsync(Window owner, AreaGeneratorHost host)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(host);

        var viewModel = new AreaGeneratorViewModel(host);
        return await new AreaGeneratorWindow(viewModel, host.Window).ShowDialog<string?>(owner).ConfigureAwait(true);
    }

    private void ApplyOptions(AreaGeneratorTexts texts, AreaGeneratorWindowOptions? options)
    {
        Title = string.IsNullOrWhiteSpace(options?.ApplicationName)
            ? texts.Get(AreaGeneratorStringId.WindowTitleBare)
            : texts.Get(AreaGeneratorStringId.WindowTitle, options.ApplicationName);
        if (options?.Icon != null)
            Icon = options.Icon;
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e) => Close(null);

    private void OnOpened(object? sender, EventArgs e)
    {
        if (DataContext is AreaGeneratorViewModel viewModel)
            viewModel.EnableAutomaticPreview();
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (DataContext is AreaGeneratorViewModel { IsBusy: true })
            e.Cancel = true;
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (DataContext is IDisposable disposable)
            disposable.Dispose();
    }
}
