using Nwn.Authoring.Documents.NimGff;

namespace Nwn.Authoring.Editing
{

    /// <summary>
    /// Memento for a single LocStringEntry's text change (LocStringEntry.SetText). Restores the
    /// exact raw string token bytes.
    /// </summary>
    public sealed class LocStringEntryTextEdit : IDocumentEdit, IDocumentEditTargetProvider
    {
        private readonly LocStringEntry _entry;
        private readonly byte[] _oldRawText;
        private readonly byte[] _newRawText;

        internal LocStringEntryTextEdit(LocStringEntry entry, byte[] oldRawText, byte[] newRawText)
        {
            _entry = entry;
            _oldRawText = oldRawText;
            _newRawText = newRawText;
        }

        public void Apply()
        {
            _entry.RawText = _newRawText;
        }

        public void Revert()
        {
            _entry.RawText = _oldRawText;
        }

        public string Describe()
        {
            return $"Set locstring text for language '{_entry.LanguageKey}'";
        }

        public IEnumerable<object> GetMutationTargets() => new object[] { _entry };
    }

}
