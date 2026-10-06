using Nwn.Authoring.Documents.NimGff;

namespace Nwn.Authoring.Editing
{
    /// <summary>Memento for inserting a struct into a list-typed field (JsonGffField.InsertElement).</summary>
    public sealed class InsertElementEdit : IDocumentEdit, IDocumentEditTargetProvider
    {
        private readonly JsonGffField _field;
        private readonly int _index;
        private readonly JsonGffStruct _element;

        internal InsertElementEdit(JsonGffField field, int index, JsonGffStruct element)
        {
            _field = field;
            _index = index;
            _element = element;
        }

        public void Apply()
        {
            _field.InsertElement(_index, _element);
        }

        public void Revert()
        {
            _field.RemoveElementAt(_index);
        }

        public string Describe()
        {
            return $"Insert list element at {_index}";
        }

        public IEnumerable<object> GetMutationTargets() =>
            ListElementMutationTargets.Enumerate(_field, _element);
    }

}
