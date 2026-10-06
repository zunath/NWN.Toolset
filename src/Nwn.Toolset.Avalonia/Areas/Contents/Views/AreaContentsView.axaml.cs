using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Nwn.Toolset.Avalonia.Areas.Contents.Views;

/// <summary>Tree view for host-provided placed-instance snapshots.</summary>
public sealed partial class AreaContentsView : UserControl
{
    public static readonly StyledProperty<AreaContentsViewModel?> ContentsProperty =
        AvaloniaProperty.Register<AreaContentsView, AreaContentsViewModel?>(nameof(Contents));

    private AreaContentsNodeViewModel? _contextRow;
    private AreaContentsViewModel? _model;
    private bool _isRevealSubscribed;

    public AreaContentsViewModel? Contents
    {
        get => GetValue(ContentsProperty);
        set => SetValue(ContentsProperty, value);
    }

    public AreaContentsView() => InitializeComponent();

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ContentsProperty)
            SetModel(change.GetNewValue<AreaContentsViewModel?>());
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        SetModel(Contents);
        SubscribeToReveal();
        QueuePendingRowReveal();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        UnsubscribeFromReveal();
        base.OnDetachedFromVisualTree(e);
    }

    private void SetModel(AreaContentsViewModel? next)
    {
        if (ReferenceEquals(next, _model))
            return;
        UnsubscribeFromReveal();
        _model = next;
        DataContext = next;
        if (_model is not null && VisualRoot is not null)
        {
            SubscribeToReveal();
            QueuePendingRowReveal();
        }
    }

    private void SubscribeToReveal()
    {
        if (_isRevealSubscribed || _model is null)
            return;
        _model.RowRevealRequested += QueuePendingRowReveal;
        _isRevealSubscribed = true;
    }

    private void UnsubscribeFromReveal()
    {
        if (!_isRevealSubscribed || _model is null)
            return;
        _model.RowRevealRequested -= QueuePendingRowReveal;
        _isRevealSubscribed = false;
    }

    private void QueuePendingRowReveal() => Dispatcher.UIThread.Post(() =>
    {
        if (VisualRoot is not null && _model?.TryTakePendingRowReveal(out var row) == true)
            RowsList.ScrollIntoView(row);
    }, DispatcherPriority.Render);

    private void OnRowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed &&
            sender is Control { DataContext: AreaContentsNodeViewModel row })
            _model!.SelectedRow = row;
    }

    private void OnRowContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        if (sender is not Control
            { DataContext: AreaContentsNodeViewModel { CanOpenProperties: true } row } owner || _model is null)
        {
            _contextRow = null;
            e.Handled = true;
            return;
        }

        _contextRow = row;
        _model.SelectedRow = row;
        if (owner.ContextMenu is { } menu && !menu.IsOpen)
            menu.Open(owner);
        e.Handled = true;
    }

    private void OnOpenPropertiesClick(object? sender, RoutedEventArgs e)
    {
        if (_contextRow is not null)
            _model?.OpenPropertiesCommand.Execute(_contextRow);
        _contextRow = null;
    }

    private void OnRowDoubleTapped(object? sender, TappedEventArgs e) =>
        _model?.OpenCommand.Execute(_model.SelectedRow);

    private void OnRowKeyDown(object? sender, KeyEventArgs e)
    {
        if (_model is null)
            return;
        if (e.Key == Key.Delete)
        {
            _model.DeleteSelectedCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            _model.OpenCommand.Execute(_model.SelectedRow);
            e.Handled = true;
        }
    }
}
