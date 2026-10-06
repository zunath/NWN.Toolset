using Nwn.Authoring.Documents.NimGff;

namespace Nwn.Authoring.Editing
{

    /// <summary>Memento for removing a struct from a list-typed field (JsonGffField.RemoveElementAt).</summary>
    public sealed class RemoveElementEdit : IDocumentEdit, IDocumentEditTargetProvider
    {
        private readonly JsonGffField _field;
        private readonly int _index;
        private readonly JsonGffStruct _element;

        internal RemoveElementEdit(JsonGffField field, int index, JsonGffStruct element)
        {
            _field = field;
            _index = index;
            _element = element;
        }

        public void Apply()
        {
            _field.RemoveElementAt(_index);
        }

        public void Revert()
        {
            _field.InsertElement(_index, _element);
        }

        public string Describe()
        {
            return $"Remove list element at {_index}";
        }

        public IEnumerable<object> GetMutationTargets() =>
            ListElementMutationTargets.Enumerate(_field, _element);
    }

}
