using Nwn.Authoring.Resources;

namespace Nwn.Authoring.Categories;

/// <summary>
/// Undo and redo stacks of folder moves. An edit leaves its stack only once the caller has persisted the
/// replay, so a refused sidecar write keeps the edit available for a retry.
/// </summary>
public sealed class CategoryMembershipHistory
{
    private readonly Stack<CategoryMembershipEdit> _undo = new();
    private readonly Stack<CategoryMembershipEdit> _redo = new();

    /// <summary>Raised whenever either stack changes, so commands can re-read their availability.</summary>
    public event Action? Changed;

    public bool CanUndo => _undo.Count > 0;

    public bool CanRedo => _redo.Count > 0;

    /// <summary>Records a completed move. A new move discards whatever could have been redone.</summary>
    public void Record(CategoryMembershipEdit edit)
    {
        ArgumentNullException.ThrowIfNull(edit);
        _undo.Push(edit);
        _redo.Clear();
        Changed?.Invoke();
    }

    /// <summary>The edit the next undo would replay, without taking it off the stack.</summary>
    public bool TryPeekUndo(out CategoryMembershipEdit edit) => _undo.TryPeek(out edit!);

    /// <summary>The edit the next redo would replay, without taking it off the stack.</summary>
    public bool TryPeekRedo(out CategoryMembershipEdit edit) => _redo.TryPeek(out edit!);

    /// <summary>Moves the peeked undo edit onto the redo stack once its replay has been persisted.</summary>
    public void CompleteUndo()
    {
        _redo.Push(_undo.Pop());
        Changed?.Invoke();
    }

    /// <summary>Moves the peeked redo edit onto the undo stack once its replay has been persisted.</summary>
    public void CompleteRedo()
    {
        _undo.Push(_redo.Pop());
        Changed?.Invoke();
    }

    /// <summary>Whether any recorded edit, on either stack, moved this resource.</summary>
    public bool Involves(ModuleResourceType type, string resRef) =>
        _undo.Concat(_redo).Any(edit =>
            edit.Type == type &&
            edit.ResRef.Equals(resRef, StringComparison.OrdinalIgnoreCase));

    /// <summary>Forgets every edit. Does nothing, and raises nothing, when both stacks are already empty.</summary>
    public void Clear()
    {
        if (_undo.Count == 0 && _redo.Count == 0)
            return;

        _undo.Clear();
        _redo.Clear();
        Changed?.Invoke();
    }
}
