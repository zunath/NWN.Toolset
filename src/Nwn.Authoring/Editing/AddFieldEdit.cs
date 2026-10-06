using Nwn.Authoring.Documents.NimGff;

namespace Nwn.Authoring.Editing
{
    /// <summary>
    /// Memento for adding a named field to a struct (JsonGffStruct.Add). Reverting removes it by
    /// name; JsonGffStruct.Add always recomputes nwn_gff's sorted insertion position from the
    /// struct's current contents, so re-applying (redo) reproduces the original position exactly.
    /// </summary>
    public sealed class AddFieldEdit : IDocumentEdit, IDocumentEditTargetProvider
    {
        private readonly JsonGffStruct _struct;
        private readonly string _name;
        private readonly JsonGffField _field;
        private readonly object _fieldMutationTarget;

        internal AddFieldEdit(JsonGffStruct owner, string name, JsonGffField field)
        {
            _struct = owner;
            _name = name;
            _field = field;
            _fieldMutationTarget = owner.GetFieldMutationTarget(name);
        }

        public void Apply()
        {
            _struct.Add(_name, _field);
        }

        public void Revert()
        {
            _struct.Remove(_name);
        }

        public string Describe()
        {
            return $"Add field '{_name}'";
        }

        public IEnumerable<object> GetMutationTargets() => new[] { _field, _fieldMutationTarget };
    }

}
