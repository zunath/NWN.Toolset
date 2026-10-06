using Nwn.Authoring.Editing;

namespace Nwn.Authoring.Areas.Editing;

/// <summary>
/// Coordinates the separate histories for an area's ARE, GIT and GIC documents. GIT and GIC
/// mutations share one transaction so their parallel instance and comment lists undo together.
/// </summary>
public sealed class AreaDocumentEditSession : IDisposable
{
    private readonly List<AreaDocumentEditTarget> _editOrder = new();
    private readonly List<AreaDocumentEditTarget> _undoneOrder = new();
    private byte[] _savedArea;
    private byte[] _savedInstances;
    private byte[] _savedComments;
    private bool _disposed;

    public DocumentSession Area { get; }
    public DocumentSession Instances { get; }
    public DocumentSession Comments { get; }

    public event Action<AreaDocumentEditTarget>? EditCommitted;
    public event Action? HistoryChanged;

    public bool IsDirty => IsAreaDirty || IsInstancesDirty || IsCommentsDirty;
    public bool IsAreaDirty => !_savedArea.AsSpan().SequenceEqual(Area.ToBytes());
    public bool IsInstancesDirty => !_savedInstances.AsSpan().SequenceEqual(Instances.ToBytes());
    public bool IsCommentsDirty => !_savedComments.AsSpan().SequenceEqual(Comments.ToBytes());
    public bool CanUndoArea => Area.UndoStack.CanUndo;
    public bool CanRedoArea => Area.UndoStack.CanRedo;
    public bool CanUndoInstances => Instances.UndoStack.CanUndo;
    public bool CanRedoInstances => Instances.UndoStack.CanRedo;
    public AreaDocumentEditTarget? LastUndoableTarget => LastUndoable();
    public AreaDocumentEditTarget? LastRedoableTarget => LastRedoable();

    public AreaDocumentEditSession(
        DocumentSession area,
        DocumentSession instances,
        DocumentSession comments)
    {
        Area = area ?? throw new ArgumentNullException(nameof(area));
        Instances = instances ?? throw new ArgumentNullException(nameof(instances));
        Comments = comments ?? throw new ArgumentNullException(nameof(comments));
        if (ReferenceEquals(area, instances) || ReferenceEquals(area, comments) ||
            ReferenceEquals(instances, comments))
            throw new ArgumentException("Each area document requires its own session.");

        _savedArea = area.ToBytes();
        _savedInstances = instances.ToBytes();
        _savedComments = comments.ToBytes();
    }

    public bool ExecuteArea(string description, Action mutation) =>
        Execute(AreaDocumentEditTarget.Area, Area, description, mutation);

    public bool ExecuteInstances(string description, Action mutation) =>
        Execute(AreaDocumentEditTarget.Instances, Instances, description, mutation);

    private bool Execute(
        AreaDocumentEditTarget target,
        DocumentSession session,
        string description,
        Action mutation)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(mutation);

        var previousPosition = session.UndoStack.Position;
        if (target == AreaDocumentEditTarget.Instances)
            session.ExecuteRelated(description, mutation, Comments);
        else
            session.Execute(description, mutation);
        if (session.UndoStack.Position == previousPosition)
            return false;

        _editOrder.Add(target);
        _undoneOrder.Clear();
        var other = target == AreaDocumentEditTarget.Area ? Instances : Area;
        other.UndoStack.DiscardRedo();
        EditCommitted?.Invoke(target);
        HistoryChanged?.Invoke();
        return true;
    }

    public bool UndoArea() => Undo(AreaDocumentEditTarget.Area, Area);

    public bool RedoArea() => Redo(AreaDocumentEditTarget.Area, Area);

    public bool UndoInstances() => Undo(AreaDocumentEditTarget.Instances, Instances);

    public bool RedoInstances() => Redo(AreaDocumentEditTarget.Instances, Instances);

    public bool UndoLatest()
    {
        var target = LastUndoable();
        if (target is null)
            return false;
        return target == AreaDocumentEditTarget.Area ? UndoArea() : UndoInstances();
    }

    public bool RedoLatest()
    {
        var target = LastRedoable();
        if (target is null)
            return false;
        return target == AreaDocumentEditTarget.Area ? RedoArea() : RedoInstances();
    }

    private bool Undo(AreaDocumentEditTarget target, DocumentSession session)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!session.UndoStack.CanUndo)
            return false;

        var index = _editOrder.LastIndexOf(target);
        if (index >= 0)
            _editOrder.RemoveAt(index);
        _undoneOrder.Add(target);
        session.Undo();
        HistoryChanged?.Invoke();
        return true;
    }

    private bool Redo(AreaDocumentEditTarget target, DocumentSession session)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!session.UndoStack.CanRedo)
            return false;

        var index = _undoneOrder.LastIndexOf(target);
        if (index >= 0)
            _undoneOrder.RemoveAt(index);
        _editOrder.Add(target);
        session.Redo();
        HistoryChanged?.Invoke();
        return true;
    }

    public AreaDocumentSaveSnapshot CaptureSaveSnapshot()
    {
        var snapshots = DocumentSession.CaptureSnapshots(Area, Instances, Comments);
        return new AreaDocumentSaveSnapshot(snapshots[0], snapshots[1], snapshots[2]);
    }

    public void AcceptSaved(AreaDocumentSaveSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        _savedArea = snapshot.AreaBytes;
        _savedInstances = snapshot.InstanceBytes;
        _savedComments = snapshot.CommentBytes;

        if (snapshot.MatchesArea(Area.ToBytes()))
            Area.UndoStack.MarkSaved();
        if (snapshot.MatchesInstances(Instances.ToBytes()))
            Instances.UndoStack.MarkSaved();
        HistoryChanged?.Invoke();
    }

    public void AcceptCurrentAsSaved()
    {
        AcceptSaved(CaptureSaveSnapshot());
    }

    public void AcceptSavedComments(byte[] savedBytes)
    {
        ArgumentNullException.ThrowIfNull(savedBytes);
        _savedComments = savedBytes.ToArray();
        HistoryChanged?.Invoke();
    }

    public void AcceptCurrentCommentsAsSaved() => AcceptSavedComments(Comments.ToBytes());

    private AreaDocumentEditTarget? LastUndoable()
    {
        for (var index = _editOrder.Count - 1; index >= 0; index--)
        {
            var target = _editOrder[index];
            var session = target == AreaDocumentEditTarget.Area ? Area : Instances;
            if (session.UndoStack.CanUndo)
                return target;
        }

        if (CanUndoInstances)
            return AreaDocumentEditTarget.Instances;
        return CanUndoArea ? AreaDocumentEditTarget.Area : null;
    }

    private AreaDocumentEditTarget? LastRedoable()
    {
        for (var index = _undoneOrder.Count - 1; index >= 0; index--)
        {
            var target = _undoneOrder[index];
            var session = target == AreaDocumentEditTarget.Area ? Area : Instances;
            if (session.UndoStack.CanRedo)
                return target;
        }

        if (CanRedoInstances)
            return AreaDocumentEditTarget.Instances;
        return CanRedoArea ? AreaDocumentEditTarget.Area : null;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Area.ThrowIfOwnedByCurrentTransaction();
        Instances.ThrowIfOwnedByCurrentTransaction();
        Comments.ThrowIfOwnedByCurrentTransaction();

        _disposed = true;
        Area.Dispose();
        Instances.Dispose();
        Comments.Dispose();
    }
}
