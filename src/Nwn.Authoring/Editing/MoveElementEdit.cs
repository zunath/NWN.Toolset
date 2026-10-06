using Nwn.Authoring.Documents.NimGff;

namespace Nwn.Authoring.Editing
{

    /// <summary>Memento for reordering a list-typed field's elements (JsonGffField.MoveElement).</summary>
    public sealed class MoveElementEdit : IDocumentEdit, IDocumentEditTargetProvider
    {
        private readonly JsonGffField _field;
        private readonly int _fromIndex;
        private readonly int _toIndex;

        internal MoveElementEdit(JsonGffField field, int fromIndex, int toIndex)
        {
            _field = field;
            _fromIndex = fromIndex;
            _toIndex = toIndex;
        }

        public void Apply()
        {
            _field.MoveElement(_fromIndex, _toIndex);
        }

        public void Revert()
        {
            _field.MoveElement(_toIndex, _fromIndex);
        }

        public string Describe()
        {
            return $"Move list element {_fromIndex} -> {_toIndex}";
        }

        public IEnumerable<object> GetMutationTargets() => new object[] { _field };
    }

}
