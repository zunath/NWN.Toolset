using Nwn.Authoring.Documents.NimGff;

namespace Nwn.Authoring.Editing
{

    /// <summary>
    /// Memento for removing a language entry from a cexolocstring field
    /// (JsonGffField.RemoveLocStringEntry). Reverting re-inserts at the original index so
    /// untouched entries' relative order (and therefore serialized bytes) is reproduced exactly.
    /// </summary>
    public sealed class RemoveLocStringEntryEdit : IDocumentEdit
    {
        private readonly JsonGffField _field;
        private readonly int _index;
        private readonly LocStringEntry _entry;

        internal RemoveLocStringEntryEdit(JsonGffField field, int index, LocStringEntry entry)
        {
            _field = field;
            _index = index;
            _entry = entry;
        }

        public void Apply()
        {
            _field.RemoveLocStringEntry(_entry.LanguageKey);
        }

        public void Revert()
        {
            _field.InsertLocStringEntry(_index, _entry);
        }

        public string Describe()
        {
            return $"Remove locstring entry '{_entry.LanguageKey}'";
        }
    }

}
