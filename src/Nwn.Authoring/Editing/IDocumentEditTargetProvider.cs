
namespace Nwn.Authoring.Editing
{

    internal interface IDocumentEditTargetProvider
    {
        IEnumerable<object> GetMutationTargets();
    }

}
