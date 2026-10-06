using Nwn.Authoring.Documents.NimGff;

namespace Nwn.Authoring.Editing
{
    /// <summary>
    /// Memento for inserting a language entry into a cexolocstring field
    /// (JsonGffField.InsertLocStringEntry / AddLocStringEntry).
    /// </summary>
    public sealed class InsertLocStringEntryEdit : IDocumentEdit
    {
        private readonly JsonGffField _field;
        private readonly int _index;
        private readonly LocStringEntry _entry;

        internal InsertLocStringEntryEdit(JsonGffField field, int index, LocStringEntry entry)
        {
            _field = field;
            _index = index;
            _entry = entry;
        }

        public void Apply()
        {
            _field.InsertLocStringEntry(_index, _entry);
        }

        public void Revert()
        {
            _field.RemoveLocStringEntry(_entry.LanguageKey);
        }

        public string Describe()
        {
            return $"Add locstring entry '{_entry.LanguageKey}'";
        }
    }

}
