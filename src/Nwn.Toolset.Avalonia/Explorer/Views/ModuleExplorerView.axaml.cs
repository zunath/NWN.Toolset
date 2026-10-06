using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Nwn.Toolset.Avalonia.Explorer.Workflow;

namespace Nwn.Toolset.Avalonia.Explorer.Views;

/// <summary>
/// The Module Contents panel. Bind its DataContext to a <see cref="ModuleExplorerController"/>. The
/// code-behind only turns pointer and keyboard gestures into controller calls: double-click to open,
/// right-click to select before the menu, drag a resource onto a folder or Unsorted, and Ctrl+Z / Ctrl+Y
/// (or Ctrl+Shift+Z) for move undo and redo.
/// </summary>
public partial class ModuleExplorerView : UserControl
{
    private const double DragThreshold = 5d;
    private const double AutoScrollEdge = 24d;
    private const double AutoScrollStep = 18d;
    private const string DragSourceClass = "drag-source";
    private const string DragTargetClass = "drag-target";

    private ExplorerNodeViewModel? _dragSource;
    private ExplorerNodeViewModel? _dropTarget;
    private Control? _dragSurface;
    private IPointer? _capturedPointer;
    private ListBoxItem? _dragSourceContainer;
    private ListBoxItem? _dropTargetContainer;
    private Point _dragStart;
    private bool _isDragging;

    public ModuleExplorerView()
    {
        InitializeComponent();
    }

    private void OnItemsDoubleTapped(object? sender, TappedEventArgs e)
    {
        // Context-menu items remain logical children of the row even though Avalonia renders the popup in
        // a separate visual tree. A quick right-click followed by a menu selection can therefore reach this
        // ListBox as a DoubleTapped gesture on Windows. Only a gesture whose visual source is actually
        // inside one of this tree's row containers may open a resource.
        if (e.Source is not Visual source ||
            (source as ListBoxItem ?? source.FindAncestorOfType<ListBoxItem>()) is not
                { DataContext: ExplorerNodeViewModel row })
        {
            return;
        }

        if (DataContext is ModuleExplorerController controller)
        {
            controller.SelectedRow = row;
            controller.OpenSelectedItem();
            e.Handled = true;
        }
    }

    /// <summary>
    /// Selects the row that was right-clicked. Avalonia does not select on right-click, and every command
    /// on the row's menu acts on the selection.
    /// </summary>
    private void OnRowContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        if (DataContext is ModuleExplorerController controller &&
            sender is Control { DataContext: ExplorerNodeViewModel row })
        {
            controller.SelectedRow = row;
        }
    }

    private void OnRowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control { DataContext: ExplorerNodeViewModel { IsResource: true } row } surface ||
            !e.GetCurrentPoint(surface).Properties.IsLeftButtonPressed)
        {
            return;
        }

        CancelRowDrag();
        _dragSource = row;
        _dragSurface = surface;
        _dragStart = e.GetPosition(ModuleTree);
        if (DataContext is ModuleExplorerController controller)
            controller.SelectedRow = row;
        _capturedPointer = e.Pointer;
        e.Pointer.Capture(surface);
    }

    private void OnModuleTreeKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _dragSource != null)
        {
            CancelRowDrag();
            e.Handled = true;
            return;
        }

        if (DataContext is not ModuleExplorerController controller)
            return;

        if (e.Key == Key.Z && e.KeyModifiers == KeyModifiers.Control &&
            controller.UndoResourceMoveCommand.CanExecute(null))
        {
            controller.UndoResourceMoveCommand.Execute(null);
            e.Handled = true;
        }
        else if (((e.Key == Key.Y && e.KeyModifiers == KeyModifiers.Control) ||
                  (e.Key == Key.Z && e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift))) &&
                 controller.RedoResourceMoveCommand.CanExecute(null))
        {
            controller.RedoResourceMoveCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void OnRowPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragSource == null || _dragSurface == null)
            return;

        if (!e.GetCurrentPoint(_dragSurface).Properties.IsLeftButtonPressed)
        {
            CancelRowDrag(e.Pointer);
            return;
        }

        var point = e.GetPosition(ModuleTree);
        if (!_isDragging)
        {
            var movedFarEnough = Math.Abs(point.X - _dragStart.X) >= DragThreshold ||
                                 Math.Abs(point.Y - _dragStart.Y) >= DragThreshold;
            if (!movedFarEnough)
                return;

            _isDragging = true;
            _dragSourceContainer = FindContainer(_dragSource);
            _dragSourceContainer?.Classes.Add(DragSourceClass);
        }

        ScrollNearEdge(point);
        UpdateDropTarget(e);
        e.Handled = true;
    }

    private void OnRowPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var source = _dragSource;
        var target = _dropTarget;
        var commit = _isDragging && source != null && target != null;

        CancelRowDrag(e.Pointer);
        if (commit && DataContext is ModuleExplorerController controller)
            controller.DropResource(source, target);
        if (commit)
            e.Handled = true;
    }

    private void OnRowPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e) =>
        CancelRowDrag();

    private void UpdateDropTarget(PointerEventArgs e)
    {
        ClearDropTarget();
        if (_dragSource == null || DataContext is not ModuleExplorerController controller)
            return;

        var container = FindContainerAt(e);
        if (container?.DataContext is not ExplorerNodeViewModel target ||
            !controller.CanDropResource(_dragSource, target))
        {
            return;
        }

        _dropTarget = target;
        _dropTargetContainer = container;
        container.Classes.Add(DragTargetClass);
    }

    private ListBoxItem? FindContainerAt(PointerEventArgs e) =>
        ModuleTree.GetVisualDescendants()
            .OfType<ListBoxItem>()
            .FirstOrDefault(item =>
            {
                var point = e.GetPosition(item);
                return point.X >= 0d && point.X <= item.Bounds.Width &&
                       point.Y >= 0d && point.Y <= item.Bounds.Height;
            });

    private ListBoxItem? FindContainer(ExplorerNodeViewModel row) =>
        ModuleTree.GetVisualDescendants()
            .OfType<ListBoxItem>()
            .FirstOrDefault(item => ReferenceEquals(item.DataContext, row));

    private void ScrollNearEdge(Point point)
    {
        var scroll = ModuleTree.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
        if (scroll == null)
            return;

        var vertical = scroll.Offset.Y;
        if (point.Y < AutoScrollEdge)
            vertical -= AutoScrollStep;
        else if (point.Y > ModuleTree.Bounds.Height - AutoScrollEdge)
            vertical += AutoScrollStep;
        else
            return;

        var maximum = Math.Max(0d, scroll.Extent.Height - scroll.Viewport.Height);
        scroll.Offset = new Vector(scroll.Offset.X, Math.Clamp(vertical, 0d, maximum));
    }

    private void ClearDropTarget()
    {
        _dropTargetContainer?.Classes.Remove(DragTargetClass);
        _dropTargetContainer = null;
        _dropTarget = null;
    }

    private void CancelRowDrag(IPointer? pointer = null)
    {
        var pointerToRelease = pointer ?? _capturedPointer;
        _dragSourceContainer?.Classes.Remove(DragSourceClass);
        ClearDropTarget();
        _dragSource = null;
        _dragSurface = null;
        _capturedPointer = null;
        _dragSourceContainer = null;
        _isDragging = false;
        pointerToRelease?.Capture(null);
    }
}
