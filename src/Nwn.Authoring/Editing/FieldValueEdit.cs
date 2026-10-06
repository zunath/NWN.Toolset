using Nwn.Authoring.Documents.NimGff;

namespace Nwn.Authoring.Editing
{
    /// <summary>
    /// Memento for a scalar JsonGffField value change (SetString/SetInteger/SetUnsignedInteger/
    /// SetSingle/SetDouble). Restores the exact raw JSON token bytes captured before and after the
    /// mutation, plus the field-level "id" (RawLocStringId) alongside it, so Apply/Revert never
    /// re-derive formatting and always reproduce the original bytes exactly.
    /// </summary>
    public sealed class FieldValueEdit : IDocumentEdit, IDocumentEditTargetProvider
    {
        private readonly JsonGffField _field;
        private readonly byte[]? _oldValue;
        private readonly byte[]? _oldLocStringId;
        private readonly byte[]? _newValue;
        private readonly byte[]? _newLocStringId;

        internal FieldValueEdit(JsonGffField field, byte[]? oldValue, byte[]? oldLocStringId,
            byte[]? newValue, byte[]? newLocStringId)
        {
            _field = field;
            _oldValue = oldValue;
            _oldLocStringId = oldLocStringId;
            _newValue = newValue;
            _newLocStringId = newLocStringId;
        }

        public void Apply()
        {
            _field.RawValue = _newValue;
            _field.RawLocStringId = _newLocStringId;
        }

        public void Revert()
        {
            _field.RawValue = _oldValue;
            _field.RawLocStringId = _oldLocStringId;
        }

        public string Describe()
        {
            return $"Set {GffFieldTypeNames.NameOf(_field.Type)} field value";
        }

        public IEnumerable<object> GetMutationTargets() => new object[] { _field };
    }

}
