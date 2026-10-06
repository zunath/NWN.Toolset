namespace Nwn.Authoring.Editing
{
    /// <summary>
    /// Provides operation-local transaction and replay state for guarded GFF mutations.
    /// Session ownership lives on the document nodes and does not depend on this ambient state.
    /// </summary>
    /// <remarks>
    /// Ownership checks run at the typed GFF mutation entry points. Legacy mutable collection
    /// views remain trusted-only and direct collection writes do not participate in this boundary.
    /// </remarks>
    public static class EditScope
    {
        private static readonly AsyncLocal<DocumentTransaction?> _currentTransaction = new();
        private static readonly AsyncLocal<int> _replayDepth = new();

        /// <summary>True while a DocumentTransaction is open on this call context.</summary>
        public static bool IsTransactionOpen => _currentTransaction.Value != null;

        /// <summary>
        /// Suppresses capture while a detached document is being constructed. Ownership checks remain
        /// active, so this scope cannot be used to mutate a document attached to an open session.
        /// </summary>
        public static IDisposable EnterConstruction()
        {
            var transaction = _currentTransaction.Value;
            _currentTransaction.Value = null;
            return new Releaser(() => _currentTransaction.Value = transaction);
        }

        /// <summary>Enters an operation-local transaction. Throws on nesting.</summary>
        internal static IDisposable EnterTransaction(DocumentTransaction transaction)
        {
            if (_currentTransaction.Value != null)
                throw new InvalidOperationException(
                    "A document transaction is already open on this call context; nested transactions are not supported. " +
                    "Commit or dispose the current transaction before beginning another.");

            _currentTransaction.Value = transaction;
            return new Releaser(() => _currentTransaction.Value = null);
        }

        /// <summary>Suppresses ownership checks and capture while applying saved edits.</summary>
        internal static IDisposable EnterReplay()
        {
            _replayDepth.Value += 1;
            return new Releaser(() => _replayDepth.Value -= 1);
        }

        /// <summary>
        /// Requires the active transaction to authorize every session that owns a registered node.
        /// Detached nodes remain unrestricted.
        /// </summary>
        internal static void EnsureMutationAllowed(object? target = null)
        {
            if (_replayDepth.Value > 0 || target == null)
                return;

            var owners = DocumentOwnershipRegistry.GetOwners(target);
            if (owners.Length == 0)
                return;

            var transaction = _currentTransaction.Value;
            foreach (var owner in owners)
            {
                if (transaction?.Authorizes(owner) == true)
                    continue;

                throw new InvalidOperationException(
                    "This document node is attached to an undo stack; mutate it inside a transaction " +
                    "opened by an owning DocumentSession.");
            }
        }

        internal static void ThrowIfTransactionOwns(DocumentOwnershipRegistration registration)
        {
            if (_currentTransaction.Value?.Authorizes(registration) == true)
                throw new InvalidOperationException(
                    "A document session cannot be disposed while an open transaction owns it.");
        }

        /// <summary>Captures an edit in the active transaction when one is open.</summary>
        internal static void Capture(IDocumentEdit edit) => Capture(null, edit);

        /// <summary>Validates ownership and captures an edit targeting the supplied node.</summary>
        internal static void Capture(object? target, IDocumentEdit edit)
        {
            if (_replayDepth.Value > 0)
                return;

            if (target != null)
                EnsureMutationAllowed(target);
            _currentTransaction.Value?.AddEdit(edit);
        }

        private sealed class Releaser : IDisposable
        {
            private Action? _dispose;

            public Releaser(Action dispose) => _dispose = dispose;

            public void Dispose() => Interlocked.Exchange(ref _dispose, null)?.Invoke();
        }
    }
}
