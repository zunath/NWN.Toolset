namespace Nwn.Authoring.Documents;

/// <summary>A bounded in-memory editing history for codec-defined mutable documents.</summary>
public sealed class DocumentHistory<TDocument> where TDocument : class
{
    private readonly IDocumentCodec<TDocument> _codec;
    private readonly DocumentSessionOptions _options;
    private readonly List<byte[]> _undo = [];
    private readonly List<byte[]> _redo = [];
    private TDocument _document;
    private byte[] _currentSnapshot;
    private byte[] _savedSnapshot;

    public bool IsDirty => !HasSavedBaseline || !_currentSnapshot.AsSpan().SequenceEqual(_savedSnapshot);
    public bool HasSavedBaseline { get; private set; }
    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    public DocumentHistory(
        TDocument document,
        IDocumentCodec<TDocument> codec,
        DocumentSessionOptions? options = null,
        bool hasSavedBaseline = true)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(codec);
        _codec = codec;
        _options = options ?? new DocumentSessionOptions();
        _options.Validate();
        _document = Clone(document);
        _currentSnapshot = EncodeSnapshot(_document);
        _savedSnapshot = _currentSnapshot.ToArray();
        HasSavedBaseline = hasSavedBaseline;
    }

    /// <summary>Inspects a detached copy; returned mutable values cannot change the history.</summary>
    public TResult Inspect<TResult>(Func<TDocument, TResult> inspect)
    {
        ArgumentNullException.ThrowIfNull(inspect);
        return inspect(Clone(_document));
    }

    /// <summary>Applies an isolated edit and records only codec-visible changes.</summary>
    public void Edit(Action<TDocument> edit)
    {
        ArgumentNullException.ThrowIfNull(edit);
        var workingCopy = Clone(_document);
        edit(workingCopy);
        Commit(workingCopy);
    }

    /// <summary>Replaces the current value using a detached copy of the supplied document.</summary>
    public void Replace(TDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        Commit(Clone(document));
    }

    public bool Undo()
    {
        if (_undo.Count == 0)
            return false;

        var previousIndex = _undo.Count - 1;
        var previous = _undo[previousIndex];
        _undo.RemoveAt(previousIndex);
        _redo.Add(_currentSnapshot.ToArray());
        _document = _codec.Decode(previous.ToArray());
        _currentSnapshot = previous.ToArray();
        EnforceHistoryBudget();
        return true;
    }

    public bool Redo()
    {
        if (_redo.Count == 0)
            return false;

        var nextIndex = _redo.Count - 1;
        var next = _redo[nextIndex];
        _redo.RemoveAt(nextIndex);
        _undo.Add(_currentSnapshot.ToArray());
        _document = _codec.Decode(next.ToArray());
        _currentSnapshot = next.ToArray();
        EnforceHistoryBudget();
        return true;
    }

    /// <summary>Acknowledges the current value as saved after its owner confirms persistence.</summary>
    public void MarkSaved()
    {
        _savedSnapshot = _currentSnapshot.ToArray();
        HasSavedBaseline = true;
    }

    /// <summary>Acknowledges the exact document value confirmed by an asynchronous persistence operation.</summary>
    public void MarkSaved(TDocument savedDocument)
    {
        ArgumentNullException.ThrowIfNull(savedDocument);
        _savedSnapshot = EncodeSnapshot(savedDocument);
        HasSavedBaseline = true;
    }

    internal byte[] CopyCurrentSnapshot() => _currentSnapshot.ToArray();

    private void Commit(TDocument document)
    {
        var after = EncodeSnapshot(document);
        if (_currentSnapshot.AsSpan().SequenceEqual(after))
            return;

        var committed = _codec.Decode(after.ToArray());
        _undo.Add(_currentSnapshot.ToArray());
        _redo.Clear();
        _document = committed;
        _currentSnapshot = after.ToArray();
        EnforceHistoryBudget();
    }

    private TDocument Clone(TDocument document) => _codec.Decode(EncodeSnapshot(document));

    private byte[] EncodeSnapshot(TDocument document) => _codec.Encode(document).ToArray();

    private void EnforceHistoryBudget()
    {
        while (_undo.Count + _redo.Count > _options.MaximumHistorySteps ||
               _undo.Concat(_redo).Sum(snapshot => (long)snapshot.Length) > _options.MaximumHistoryBytes)
        {
            if (_undo.Count > 0)
                _undo.RemoveAt(0);
            else if (_redo.Count > 0)
                _redo.RemoveAt(0);
            else
                return;
        }
    }
}
