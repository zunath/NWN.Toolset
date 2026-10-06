using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Documents;

namespace Nwn.Authoring.Editing
{
    /// <summary>
    /// Binds a file path, its parsed <see cref="JsonGffDocument"/>, and an <see cref="UndoStack"/>
    /// together for one editing session. While the session is open, mutations to nodes in its
    /// document require a transaction opened via <see cref="Begin"/>; dispose the session to release
    /// its ownership registration.
    /// </summary>
    public sealed class DocumentSession : IDisposable
    {
        private static long _nextLockOrder;
        private readonly DocumentOwnershipRegistration _ownershipRegistration;
        private readonly IDocumentCodec<JsonGffDocument> _codec;
        private readonly object _syncRoot = new();
        private readonly long _lockOrder = Interlocked.Increment(ref _nextLockOrder);
        private DateTime? _loadedMTimeUtc;
        private byte[]? _loadedContentHash;
        private bool _disposed;

        public string FilePath { get; private set; }

        public JsonGffDocument Document { get; }

        public UndoStack UndoStack { get; }

        /// <summary>Binds an already-parsed document to a path, recording the file's current mtime (if it exists) for HasExternalChange().</summary>
        public DocumentSession(string filePath, JsonGffDocument document)
            : this(filePath, document, loadedContent: null, new NimGffDocumentCodec())
        {
        }

        private DocumentSession(string filePath, JsonGffDocument document, byte[]? loadedContent, IDocumentCodec<JsonGffDocument> codec)
        {
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            Document = document ?? throw new ArgumentNullException(nameof(document));
            _codec = codec ?? throw new ArgumentNullException(nameof(codec));
            UndoStack = new UndoStack();
            var fileExists = File.Exists(filePath);
            _loadedMTimeUtc = fileExists
                ? File.GetLastWriteTimeUtc(filePath)
                : loadedContent != null
                    ? DateTime.MinValue
                    : null;
            _loadedContentHash = loadedContent != null
                ? System.Security.Cryptography.SHA256.HashData(loadedContent)
                : fileExists
                    ? System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(filePath))
                    : null;
            _ownershipRegistration = DocumentOwnershipRegistry.Register(document);
            UndoStack.BindOwner(_ownershipRegistration);
        }

        /// <summary>Loads and parses the file at the given path into a new session.</summary>
        public static DocumentSession Open(string filePath, IDocumentCodec<JsonGffDocument>? codec = null)
        {
            codec ??= new NimGffDocumentCodec();
            var content = File.ReadAllBytes(filePath);
            return FromLoadedContent(filePath, codec.Decode(content), content, codec);
        }

        /// <summary>
        /// Binds bytes and a document already parsed on a worker thread. Ownership is attached to
        /// the parsed graph and therefore does not depend on the caller's execution context.
        /// </summary>
        public static DocumentSession FromLoadedContent(
            string filePath,
            JsonGffDocument document,
            byte[] loadedContent,
            IDocumentCodec<JsonGffDocument>? codec = null)
        {
            ArgumentNullException.ThrowIfNull(loadedContent);
            return new DocumentSession(filePath, document, loadedContent, codec ?? new NimGffDocumentCodec());
        }

        /// <summary>Begins a transaction on this session's undo stack.</summary>
        public DocumentTransaction Begin(string description)
            => BeginRelated(description);

        /// <summary>
        /// Begins one transaction on this session's undo stack while explicitly authorizing edits
        /// to the listed related sessions. The related documents' edits are recorded in this stack.
        /// </summary>
        /// <remarks>
        /// Related sessions must remain alive while this stack can undo or redo the grouped edit;
        /// dispose the related sessions with the owning editor session.
        /// </remarks>
        public DocumentTransaction BeginRelated(string description, params DocumentSession[] relatedSessions)
        {
            ArgumentNullException.ThrowIfNull(relatedSessions);
            ObjectDisposedException.ThrowIf(_disposed, this);

            var sessions = new List<DocumentSession> { this };
            foreach (var relatedSession in relatedSessions)
            {
                ArgumentNullException.ThrowIfNull(relatedSession);
                ObjectDisposedException.ThrowIf(relatedSession._disposed, relatedSession);
                if (!sessions.Contains(relatedSession, ReferenceEqualityComparer.Instance))
                    sessions.Add(relatedSession);
            }

            var ordered = sessions.OrderBy(session => session._lockOrder).ToArray();
            var locked = new List<DocumentSession>(ordered.Length);
            try
            {
                foreach (var session in ordered)
                {
                    Monitor.Enter(session._syncRoot);
                    locked.Add(session);
                }

                foreach (var session in sessions)
                    ObjectDisposedException.ThrowIf(session._disposed, session);

                var owners = sessions.Select(session => session._ownershipRegistration).ToArray();
                return UndoStack.BeginOwned(
                    description,
                    new Releaser(() =>
                    {
                        for (var index = locked.Count - 1; index >= 0; index--)
                            Monitor.Exit(locked[index]._syncRoot);
                    }),
                    owners);
            }
            catch
            {
                for (var index = locked.Count - 1; index >= 0; index--)
                    Monitor.Exit(locked[index]._syncRoot);
                throw;
            }
        }

        /// <summary>
        /// Runs one grouped edit and commits it as a single undo step. If the mutation throws,
        /// every edit captured before the failure is rolled back and the exception is rethrown.
        /// </summary>
        public void Execute(string description, Action mutation)
            => ExecuteWithTransaction(Begin(description), mutation);

        /// <summary>Runs one undoable edit that may also mutate explicitly related sessions.</summary>
        /// <remarks>
        /// Related sessions must remain alive while this stack can undo or redo the grouped edit;
        /// dispose the related sessions with the owning editor session.
        /// </remarks>
        public void ExecuteRelated(
            string description,
            Action mutation,
            params DocumentSession[] relatedSessions) =>
            ExecuteWithTransaction(BeginRelated(description, relatedSessions), mutation);

        private static void ExecuteWithTransaction(DocumentTransaction transaction, Action mutation)
        {
            ArgumentNullException.ThrowIfNull(mutation);

            using (transaction)
            {
                try
                {
                    mutation();
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// Runs deferred work and merges its captured edits into the originating applied edit.
        /// Returns false without mutating when that origin has left the applied history.
        /// </summary>
        public bool ExecuteCoalesced(
            IDocumentEdit origin,
            string description,
            Action mutation)
        {
            ArgumentNullException.ThrowIfNull(origin);
            ArgumentNullException.ThrowIfNull(mutation);

            lock (_syncRoot)
            {
                if (!UndoStack.ContainsApplied(origin))
                    return false;

                using var transaction = Begin(description);
                try
                {
                    mutation();
                    return transaction.CommitCoalescedInto(origin);
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// Applies derived metadata without adding an undo step. The caller must use this only for
        /// values fully determined by the document's authored content, such as a saved word count.
        /// </summary>
        public void ExecuteDerived(Action mutation)
        {
            ArgumentNullException.ThrowIfNull(mutation);

            lock (_syncRoot)
            {
                using (EditScope.EnterReplay())
                    mutation();
            }
        }

        /// <summary>
        /// True if the file at FilePath changed since this session loaded or last saved (deleted,
        /// newly created, different last-write time, or - because timestamp granularity can be
        /// coarse and external tools may preserve mtimes - different content under the same
        /// timestamp, decided by fingerprint).
        /// </summary>
        public bool HasExternalChange()
        {
            lock (_syncRoot)
            {
                if (!File.Exists(FilePath))
                    return _loadedMTimeUtc != null;

                if (File.GetLastWriteTimeUtc(FilePath) != _loadedMTimeUtc)
                    return true;

                if (_loadedContentHash == null)
                    return true;

                return !System.Security.Cryptography.SHA256
                    .HashData(File.ReadAllBytes(FilePath))
                    .AsSpan()
                    .SequenceEqual(_loadedContentHash);
            }
        }

        /// <summary>
        /// Captures the content fingerprint established when this session loaded or last saved,
        /// but only while the file still matches it. Multi-step destructive operations carry this
        /// immutable baseline through their final commit check instead of trusting an earlier
        /// external-change check.
        /// </summary>
        public bool TryCaptureUnchangedFileContentHash(out byte[] contentHash)
        {
            lock (_syncRoot)
            {
                contentHash = Array.Empty<byte>();
                if (_loadedMTimeUtc == null ||
                    _loadedContentHash == null ||
                    !File.Exists(FilePath) ||
                    File.GetLastWriteTimeUtc(FilePath) != _loadedMTimeUtc)
                {
                    return false;
                }

                var currentHash = System.Security.Cryptography.SHA256
                    .HashData(File.ReadAllBytes(FilePath));
                if (!currentHash.AsSpan().SequenceEqual(_loadedContentHash))
                    return false;

                contentHash = _loadedContentHash.ToArray();
                return true;
            }
        }

        /// <summary>
        /// Reloads the file into the existing document object, clears undo/redo history, and
        /// records the reloaded file state as the new external-change baseline.
        /// </summary>
        public void ReloadFromDisk()
        {
            var content = File.ReadAllBytes(FilePath);
            ReloadFrom(_codec.Decode(content), content);
        }

        /// <summary>
        /// Replaces this session's content with an already-parsed reload. Callers reloading a
        /// multi-file group parse every member first, then commit them through this - so one
        /// malformed member cannot leave the group half-reloaded.
        /// </summary>
        public void ReloadFrom(JsonGffDocument document)
        {
            ReloadFrom(document, _codec.Encode(document));
        }

        /// <summary>
        /// Replaces this session's content and ties the baseline to the exact bytes that produced it.
        /// If the file changes after those bytes were read, the next external-change check sees the
        /// mismatch instead of accepting the newer disk generation as this document's baseline.
        /// </summary>
        public void ReloadFrom(JsonGffDocument document, byte[] loadedContent)
        {
            ArgumentNullException.ThrowIfNull(document);
            ArgumentNullException.ThrowIfNull(loadedContent);
            lock (_syncRoot)
            {
                Document.ReplaceWith(document);
                UndoStack.Reset();
                RecordCurrentFileState(loadedContent);
            }
        }

        /// <summary>
        /// Accepts the current on-disk generation as a compare-and-swap baseline, such as after the
        /// user explicitly chooses Overwrite. Successful saves should use the byte[] overload so a
        /// replacement racing the post-save bookkeeping cannot be adopted accidentally.
        /// </summary>
        public void RecordCurrentFileState()
        {
            lock (_syncRoot)
            {
                _loadedMTimeUtc = File.Exists(FilePath) ? File.GetLastWriteTimeUtc(FilePath) : null;
                _loadedContentHash = _loadedMTimeUtc != null
                    ? System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(FilePath))
                    : null;
            }
        }

        /// <summary>
        /// Records a successful save while keeping the baseline hash tied to the exact bytes written.
        /// The timestamp is still sampled from disk, but a replacement that lands before that sample
        /// cannot hide because its content will differ from this immutable hash.
        /// </summary>
        public void RecordCurrentFileState(byte[] savedContent)
        {
            ArgumentNullException.ThrowIfNull(savedContent);
            lock (_syncRoot)
            {
                _loadedMTimeUtc = File.Exists(FilePath)
                    ? File.GetLastWriteTimeUtc(FilePath)
                    : DateTime.MinValue;
                _loadedContentHash = System.Security.Cryptography.SHA256.HashData(savedContent);
            }
        }

        /// <summary>
        /// Rebinds this session to a new path after its file has been renamed on disk. The document
        /// and undo history carry over unchanged; only the identity and the external-change baseline
        /// move, so a save that renames stays one operation rather than a close-and-reopen that
        /// discards history.
        /// </summary>
        public void MoveTo(string newPath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(newPath);

            lock (_syncRoot)
            {
                FilePath = newPath;
                _loadedMTimeUtc = File.Exists(newPath) ? File.GetLastWriteTimeUtc(newPath) : null;
            }
        }

        /// <summary>Serializes this document while excluding edits and undo/redo replay.</summary>
        public byte[] ToBytes()
        {
            lock (_syncRoot)
                return _codec.Encode(Document);
        }

        /// <summary>Undoes one transaction while excluding snapshot serialization.</summary>
        public void Undo()
        {
            lock (_syncRoot)
                UndoStack.Undo();
        }

        /// <summary>Redoes one transaction while excluding snapshot serialization.</summary>
        public void Redo()
        {
            lock (_syncRoot)
                UndoStack.Redo();
        }

        /// <summary>Restores the last saved undo position while holding the document lock.</summary>
        public bool RestoreSaved()
        {
            lock (_syncRoot)
                return UndoStack.RestoreSaved();
        }

        /// <summary>
        /// Unwinds every edit made since the last save - what an editor's Revert action means.
        /// </summary>
        /// <remarks>
        /// When the saved history position was discarded by branching (save, undo past it, then a
        /// new edit), the beginning of history is NOT the saved baseline - the disk is. Falling
        /// back to undo-everything let a following Save overwrite previously committed work with
        /// the initial load state, so the discarded-marker case reloads the on-disk document.
        /// </remarks>
        public void RevertToSaved()
        {
            lock (_syncRoot)
            {
                if (UndoStack.RestoreSaved())
                    return;

                ReloadFromDisk();
            }
        }

        /// <summary>
        /// Serializes several sessions under a stable lock order, producing a mutually consistent
        /// immutable snapshot without reading a live document graph on a worker thread.
        /// </summary>
        public static byte[][] CaptureSnapshots(params DocumentSession[] sessions)
        {
            ArgumentNullException.ThrowIfNull(sessions);
            if (sessions.Any(session => session == null))
                throw new ArgumentException("Snapshot sessions cannot contain null.", nameof(sessions));

            var ordered = sessions
                .Distinct()
                .OrderBy(session => session._lockOrder)
                .ToArray();
            byte[][]? snapshots = null;

            void CaptureUnderLock(int index)
            {
                if (index == ordered.Length)
                {
                    snapshots = sessions.Select(session => session._codec.Encode(session.Document)).ToArray();
                    return;
                }

                lock (ordered[index]._syncRoot)
                    CaptureUnderLock(index + 1);
            }

            CaptureUnderLock(0);
            return snapshots!;
        }

        /// <summary>Releases this session's registrations from its document graph.</summary>
        internal void ThrowIfOwnedByCurrentTransaction() =>
            EditScope.ThrowIfTransactionOwns(_ownershipRegistration);

        public void Dispose()
        {
            lock (_syncRoot)
            {
                if (_disposed)
                    return;

                ThrowIfOwnedByCurrentTransaction();

                _disposed = true;
                UndoStack.ReleaseOwner(_ownershipRegistration);
                _ownershipRegistration.Dispose();
            }
        }

        private sealed class Releaser : IDisposable
        {
            private Action? _release;

            public Releaser(Action release)
            {
                _release = release;
            }

            public void Dispose()
            {
                Interlocked.Exchange(ref _release, null)?.Invoke();
            }
        }
    }

}
