using Nwn.Authoring.Documents.NimGff;

namespace Nwn.Authoring.Editing
{

    /// <summary>
    /// Structural list edits own both their list and the subtree whose presence they change.
    /// A later edit to a field inside an inserted or removed element therefore overlaps the
    /// structural edit even though the two mementos hold different object references.
    /// </summary>
    internal static class ListElementMutationTargets
    {
        internal static IEnumerable<object> Enumerate(JsonGffField listField, JsonGffStruct element)
        {
            yield return listField;
            foreach (var target in Enumerate(element))
                yield return target;
        }

        private static IEnumerable<object> Enumerate(JsonGffStruct element)
        {
            yield return element;
            foreach (var (_, field) in element.Entries)
            {
                yield return field;
                switch (field.Type)
                {
                    case GffFieldType.Struct:
                        foreach (var target in Enumerate(field.Struct!))
                            yield return target;
                        break;
                    case GffFieldType.List:
                        foreach (var child in field.Elements!)
                        foreach (var target in Enumerate(child))
                            yield return target;
                        break;
                    case GffFieldType.CExoLocString:
                        foreach (var entry in field.LocStringEntries!)
                            yield return entry;
                        break;
                }
            }
        }
    }

}
