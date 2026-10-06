using System.Security.Cryptography;

namespace Nwn.Authoring.Documents;

/// <summary>A mutable authoring session with isolated edits, snapshot undo/redo, dirty tracking,
/// external-change detection, and atomic save.</summary>
public sealed class DocumentSession<TDocument> where TDocument : class
{
    private readonly IDocumentStorage _storage;
    private readonly DocumentHistory<TDocument> _history;
    private byte[]? _originalBytes;
    private string? _savedDiskHash;

    public string Path { get; }
    public bool IsDirty => _history.IsDirty;
    public bool CanUndo => _history.CanUndo;
    public bool CanRedo => _history.CanRedo;
    /// <summary>The SHA-256 hash of the file version opened or last saved by this session; null
    /// until a newly created document is first saved.</summary>
    public string? PersistedFileHash => _savedDiskHash;

    private DocumentSession(
        string path,
        DocumentHistory<TDocument> history,
        byte[]? originalBytes,
        string? savedDiskHash,
        IDocumentStorage storage)
    {
        Path = System.IO.Path.GetFullPath(path);
        _history = history;
        _originalBytes = originalBytes?.ToArray();
        _savedDiskHash = savedDiskHash;
        _storage = storage;
    }

    public static DocumentSession<TDocument> Open(
        string path,
        IDocumentCodec<TDocument> codec,
        IDocumentStorage? storage = null,
        DocumentSessionOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(codec);
        storage ??= new FileDocumentStorage();
        options ??= new DocumentSessionOptions();
        var fullPath = System.IO.Path.GetFullPath(path);
        var bytes = storage.ReadIfExists(fullPath)
            ?? throw new FileNotFoundException("The document to open does not exist.", fullPath);
        var document = codec.Decode(bytes);
        var history = new DocumentHistory<TDocument>(document, codec, options);
        return new DocumentSession<TDocument>(fullPath, history, bytes, Hash(bytes), storage);
    }

    public static DocumentSession<TDocument> Create(
        string path,
        TDocument document,
        IDocumentCodec<TDocument> codec,
        IDocumentStorage? storage = null,
        DocumentSessionOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(codec);
        storage ??= new FileDocumentStorage();
        options ??= new DocumentSessionOptions();
        var fullPath = System.IO.Path.GetFullPath(path);
        if (storage.ReadIfExists(fullPath) is not null)
            throw new IOException($"Cannot create document '{fullPath}' because it already exists.");
        var history = new DocumentHistory<TDocument>(document, codec, options, hasSavedBaseline: false);
        return new DocumentSession<TDocument>(fullPath, history, null, null, storage);
    }

    /// <summary>Reads a detached copy. Mutating or retaining this value cannot change the session.</summary>
    public TResult Inspect<TResult>(Func<TDocument, TResult> inspect)
    {
        ArgumentNullException.ThrowIfNull(inspect);
        return _history.Inspect(inspect);
    }

    /// <summary>Applies an isolated edit. Only a codec-visible change creates an undo step.</summary>
    public void Edit(Action<TDocument> edit)
    {
        _history.Edit(edit);
    }

    public bool Undo()
    {
        return _history.Undo();
    }

    public bool Redo()
    {
        return _history.Redo();
    }

    /// <summary>Saves only after verifying the known on-disk hash. Filesystem APIs do not offer an
    /// atomic content-hash compare-and-replace against noncooperating external writers; the default
    /// storage serializes cooperating sessions with a per-path cross-process mutex. A clean opened
    /// document is left byte-for-byte untouched, including its original JSON formatting.</summary>
    public void Save()
    {
        var diskBytes = _storage.ReadIfExists(Path);
        var diskHash = diskBytes is null ? null : Hash(diskBytes);
        if (!string.Equals(diskHash, _savedDiskHash, StringComparison.Ordinal))
            throw new DocumentConflictException(Path);
        if (!IsDirty && _originalBytes is not null)
            return;

        var currentSnapshot = _history.CopyCurrentSnapshot();
        var bytesToWrite = IsDirty ? currentSnapshot : _originalBytes?.ToArray() ?? currentSnapshot;
        _storage.WriteAtomically(Path, bytesToWrite, _savedDiskHash);
        _savedDiskHash = Hash(bytesToWrite);
        _originalBytes = bytesToWrite.ToArray();
        _history.MarkSaved();
    }

    private static string Hash(ReadOnlySpan<byte> bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
}
