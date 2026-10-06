using Nwn.Authoring.Documents.NimGff;

namespace Nwn.Authoring.Editing
{

    /// <summary>
    /// Memento for replacing a cexolocstring field's complete value — its strref and its whole
    /// entry list — with a copy of another localized string (LocString.CopyFrom).
    /// </summary>
    public sealed class LocStringReplaceEdit : IDocumentEdit
    {
        private readonly JsonGffField _field;
        private readonly byte[]? _oldLocStringId;
        private readonly List<LocStringEntry>? _oldEntries;
        private readonly byte[]? _newLocStringId;
        private readonly List<LocStringEntry>? _newEntries;

        internal LocStringReplaceEdit(
            JsonGffField field,
            byte[]? oldLocStringId, List<LocStringEntry>? oldEntries,
            byte[]? newLocStringId, List<LocStringEntry>? newEntries)
        {
            _field = field;
            _oldLocStringId = oldLocStringId;
            _oldEntries = oldEntries;
            _newLocStringId = newLocStringId;
            _newEntries = newEntries;
        }

        public void Apply()
        {
            _field.RawLocStringId = _newLocStringId;
            _field.LocStringEntries = _newEntries;
        }

        public void Revert()
        {
            _field.RawLocStringId = _oldLocStringId;
            _field.LocStringEntries = _oldEntries;
        }

        public string Describe()
        {
            return "Replace localized string";
        }
    }

}
