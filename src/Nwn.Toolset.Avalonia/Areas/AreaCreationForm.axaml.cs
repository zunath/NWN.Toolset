using Avalonia.Controls;
using Avalonia;
using Avalonia.Media;

namespace Nwn.Toolset.Avalonia.Areas;

/// <summary>Reusable presentation for host-owned area creation state.</summary>
public sealed partial class AreaCreationForm : UserControl
{
    public static readonly StyledProperty<IBrush?> StatusForegroundProperty =
        AvaloniaProperty.Register<AreaCreationForm, IBrush?>(nameof(StatusForeground));

    public IBrush? StatusForeground
    {
        get => GetValue(StatusForegroundProperty);
        set => SetValue(StatusForegroundProperty, value);
    }

    public AreaCreationForm()
    {
        Background = new SolidColorBrush(Color.Parse("#191E26"));
        BorderBrush = new SolidColorBrush(Color.Parse("#333C4A"));
        StatusForeground = new SolidColorBrush(Color.Parse("#D9A155"));
        InitializeComponent();
    }
}
