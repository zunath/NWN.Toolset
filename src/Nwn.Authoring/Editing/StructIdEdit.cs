using Nwn.Authoring.Documents.NimGff;

namespace Nwn.Authoring.Editing
{

    /// <summary>
    /// Memento for rewriting a struct's "__struct_id" (JsonGffStruct.SetStructId). Holds the raw
    /// token either side of the change rather than a parsed number, so reverting restores the
    /// source file's exact bytes.
    /// </summary>
    public sealed class StructIdEdit : IDocumentEdit
    {
        private readonly JsonGffStruct _struct;
        private readonly byte[]? _oldValue;
        private readonly byte[]? _newValue;

        internal StructIdEdit(JsonGffStruct owner, byte[]? oldValue, byte[]? newValue)
        {
            _struct = owner;
            _oldValue = oldValue;
            _newValue = newValue;
        }

        public void Apply()
        {
            _struct.RawStructId = _newValue;
        }

        public void Revert()
        {
            _struct.RawStructId = _oldValue;
        }

        public string Describe()
        {
            return "Renumber list element";
        }
    }

}
