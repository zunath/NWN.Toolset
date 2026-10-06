using Nwn.Authoring.Documents.NimGff;

namespace Nwn.Authoring.Editing
{

    /// <summary>
    /// Memento for removing a named field from a struct (JsonGffStruct.Remove). Reverting
    /// re-adds the exact same field instance; see <see cref="AddFieldEdit"/> for why this
    /// reproduces the original position.
    /// </summary>
    public sealed class RemoveFieldEdit : IDocumentEdit, IDocumentEditTargetProvider
    {
        private readonly JsonGffStruct _struct;
        private readonly string _name;
        private readonly JsonGffField _field;
        private readonly object _fieldMutationTarget;

        internal RemoveFieldEdit(JsonGffStruct owner, string name, JsonGffField field)
        {
            _struct = owner;
            _name = name;
            _field = field;
            _fieldMutationTarget = owner.GetFieldMutationTarget(name);
        }

        public void Apply()
        {
            _struct.Remove(_name);
        }

        public void Revert()
        {
            _struct.Add(_name, _field);
        }

        public string Describe()
        {
            return $"Remove field '{_name}'";
        }

        public IEnumerable<object> GetMutationTargets() => new[] { _field, _fieldMutationTarget };
    }

}
