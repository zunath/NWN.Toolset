using Nwn.Authoring.Areas.Placement;
using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Resources;

namespace Nwn.Authoring.Areas.Editing;

/// <summary>Edits one area's placed-instance lists and their index-aligned GIC comments.</summary>
public sealed class AreaInstanceEditor
{
    private readonly AreaDocumentEditSession _documents;

    public AreaInstanceEditor(AreaDocumentEditSession documents) =>
        _documents = documents ?? throw new ArgumentNullException(nameof(documents));

    public bool Add(ModuleResourceType type, JsonGffStruct instance, string? comment = null)
    {
        ArgumentNullException.ThrowIfNull(instance);
        if (!TryGetListField(type, out var listFieldName))
            return false;

        return _documents.ExecuteInstances($"Add {type} instance", () =>
        {
            var list = GetOrCreateInstanceList(listFieldName);
            var index = list.Elements!.Count;
            list.InsertElement(index, instance);
            new GicDocument(_documents.Comments.Document).InsertBlankComment(
                listFieldName, type, index, list.Elements.Count);
            if (comment != null)
                _documents.Comments.Document.Root.Get(listFieldName).Elements![index]
                    .Get("Comment").SetString(comment);
        });
    }

    public bool AddCopied(
        ModuleResourceType type,
        JsonGffStruct instance,
        JsonGffStruct? comment = null)
    {
        ArgumentNullException.ThrowIfNull(instance);
        if (!TryGetListField(type, out var listFieldName))
            return false;

        var copy = InstanceFieldMap.Duplicate(instance);
        return _documents.ExecuteInstances($"Paste {type} instance", () =>
        {
            var list = GetOrCreateInstanceList(listFieldName);
            var index = list.Elements!.Count;
            list.InsertElement(index, copy);
            new GicDocument(_documents.Comments.Document).InsertCopiedComment(
                listFieldName, type, index, list.Elements.Count, comment);
        });
    }

    public bool Duplicate(ModuleResourceType type, int index)
    {
        if (!TryGetListField(type, out var listFieldName) ||
            GetInstanceList(listFieldName)?.Elements is not { } elements ||
            index < 0 || index >= elements.Count)
            return false;

        return _documents.ExecuteInstances($"Duplicate {type} instance", () =>
        {
            var list = GetInstanceList(listFieldName)!;
            list.InsertElement(index + 1, InstanceFieldMap.Duplicate(list.Elements![index]));
            new GicDocument(_documents.Comments.Document).DuplicateComment(
                listFieldName, type, index, list.Elements.Count);
        });
    }

    public bool Delete(ModuleResourceType type, IReadOnlyList<int> indices)
    {
        ArgumentNullException.ThrowIfNull(indices);
        if (!TryGetListField(type, out var listFieldName) || indices.Count == 0)
            return false;

        var ordered = indices.Distinct().OrderByDescending(index => index).ToArray();
        return _documents.ExecuteInstances(
            ordered.Length == 1 ? $"Delete {type} instance" : $"Delete {ordered.Length} {type} instances",
            () =>
            {
                var list = GetInstanceList(listFieldName);
                if (list?.Elements == null)
                    return;

                var comments = new GicDocument(_documents.Comments.Document);
                foreach (var index in ordered)
                {
                    if (index < 0 || index >= list.Elements.Count)
                        continue;

                    list.RemoveElementAt(index);
                    comments.RemoveComment(listFieldName, type, index, list.Elements.Count);
                }
            });
    }

    public bool SetPosition(
        ModuleResourceType type,
        int index,
        float x,
        float y,
        float z,
        string? description = null) =>
        EditInstance(type, index, description ?? $"Move {type} instance", instance =>
            InstanceFieldMap.SetPosition(type, instance, x, y, z));

    public bool SetOrientation(
        ModuleResourceType type,
        int index,
        float xOrientation,
        float yOrientation,
        string? description = null) =>
        EditInstance(type, index, description ?? $"Rotate {type} instance", instance =>
            InstanceFieldMap.SetOrientation(type, instance, xOrientation, yOrientation));

    public bool SetTransform(
        ModuleResourceType type,
        int index,
        float x,
        float y,
        float z,
        float xOrientation,
        float yOrientation,
        string? description = null) =>
        EditInstance(type, index, description ?? $"Move {type} instance", instance =>
        {
            InstanceFieldMap.SetPosition(type, instance, x, y, z);
            InstanceFieldMap.SetOrientation(type, instance, xOrientation, yOrientation);
        });

    public JsonGffStruct? CopyInstance(ModuleResourceType type, int index)
    {
        if (!TryGetListField(type, out var listFieldName) ||
            GetInstanceList(listFieldName)?.Elements is not { } elements ||
            index < 0 || index >= elements.Count)
            return null;

        return InstanceFieldMap.Duplicate(elements[index]);
    }

    private bool EditInstance(
        ModuleResourceType type,
        int index,
        string description,
        Action<JsonGffStruct> mutation)
    {
        if (!TryGetListField(type, out var listFieldName) ||
            GetInstanceList(listFieldName)?.Elements is not { } elements ||
            index < 0 || index >= elements.Count)
            return false;

        return _documents.ExecuteInstances(description, () => mutation(elements[index]));
    }

    private JsonGffField? GetInstanceList(string name) =>
        _documents.Instances.Document.Root.GetOrNull(name);

    private JsonGffField GetOrCreateInstanceList(string name)
    {
        var list = GetInstanceList(name);
        if (list != null)
            return list;

        list = JsonGffField.CreateList();
        _documents.Instances.Document.Root.Add(name, list);
        return list;
    }

    private static bool TryGetListField(ModuleResourceType type, out string name)
    {
        name = type switch
        {
            ModuleResourceType.Utc => "Creature List",
            ModuleResourceType.Uti => "List",
            ModuleResourceType.Utp => "Placeable List",
            ModuleResourceType.Utd => "Door List",
            ModuleResourceType.Utw => "WaypointList",
            ModuleResourceType.Utm => "StoreList",
            ModuleResourceType.Uts => "SoundList",
            ModuleResourceType.Utt => "TriggerList",
            _ => string.Empty
        };
        return name.Length > 0;
    }
}
