using Nwn.Authoring.Areas.Placement;
using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Editing;
using Nwn.Authoring.Resources;

namespace Nwn.Authoring.Tests.Areas.Placement;

[TestClass]
public sealed class BlueprintInstanceSynchronizerTests
{
    [TestMethod]
    public void Synchronize_RefreshesBlueprintFieldsAndPreservesPlacementData()
    {
        foreach (var type in new[]
        {
            ModuleResourceType.Utc, ModuleResourceType.Utd, ModuleResourceType.Uti,
            ModuleResourceType.Utp, ModuleResourceType.Uts, ModuleResourceType.Utm,
            ModuleResourceType.Utt, ModuleResourceType.Utw
        })
            AssertSynchronizationPreservesPlacement(type);
    }

    private static void AssertSynchronizationPreservesPlacement(ModuleResourceType type)
    {
        var oldBlueprint = Blueprint(type, "source", "old template data");
        var newBlueprint = Blueprint(type, "target", "updated template data");
        var instance = InstanceFieldMap.CreateInstance(type, oldBlueprint, "source", 4, 8, 2, 0, 1);
        InstanceFieldMap.SetPosition(type, instance, 17.25f, -9.5f, 3.75f);

        using (EditScope.EnterConstruction())
        {
            if (type != ModuleResourceType.Utm)
            {
                instance.Add("VisualTransform", TransformField(2));
                instance.Add("VisTransformList", TransformListField(3));
            }
            if (type == ModuleResourceType.Utt)
                InstanceFieldMap.SetTriggerGeometrySize(instance, 7, 3);
        }

        var originalPosition = InstanceFieldMap.GetPosition(type, instance);
        var originalOrientation = InstanceFieldMap.GetOrientation(type, instance);
        var originalGeometry = type == ModuleResourceType.Utt
            ? FieldAsStruct(instance.Get("Geometry"))
            : null;
        var originalVisualTransform = type == ModuleResourceType.Utm ? null : FieldAsStruct(instance.Get("VisualTransform"));
        var originalAnimatedTransform = type == ModuleResourceType.Utm ? null : FieldAsStruct(instance.Get("VisTransformList"));
        var instances = JsonGffField.CreateList();
        var gitRoot = new JsonGffStruct();
        instances.InsertElement(0, instance);
        gitRoot.Add(BlueprintInstanceSynchronizer.ListFieldName(type), instances);
        var git = new JsonGffDocument("GIT ", gitRoot);

        var changed = BlueprintInstanceSynchronizer.Synchronize(
            type, newBlueprint, git, "source", "target");

        Assert.AreEqual(1, changed);
        var refreshed = git.Root.Get(BlueprintInstanceSynchronizer.ListFieldName(type)).Elements!.Single();
        Assert.AreEqual("updated template data", refreshed.GetStringOrNull("BlueprintMarker"));
        Assert.AreEqual("target", InstanceFieldMap.GetTemplateResRef(type, refreshed));
        Assert.AreEqual(originalPosition, InstanceFieldMap.GetPosition(type, refreshed));
        Assert.AreEqual(originalOrientation, InstanceFieldMap.GetOrientation(type, refreshed));
        if (originalVisualTransform != null)
            Assert.IsTrue(StoreInstanceSynchronizer.Equivalent(originalVisualTransform, FieldAsStruct(refreshed.Get("VisualTransform"))));
        if (originalAnimatedTransform != null)
            Assert.IsTrue(StoreInstanceSynchronizer.Equivalent(originalAnimatedTransform, FieldAsStruct(refreshed.Get("VisTransformList"))));
        if (originalGeometry != null)
            Assert.IsTrue(StoreInstanceSynchronizer.Equivalent(originalGeometry, FieldAsStruct(refreshed.Get("Geometry"))));
    }

    [TestMethod]
    public void ListFieldName_RejectsUnplacedResourceTypes()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => BlueprintInstanceSynchronizer.ListFieldName(ModuleResourceType.Nss));
    }

    private static JsonGffDocument Blueprint(ModuleResourceType type, string resRef, string marker)
    {
        var root = new JsonGffStruct();
        var resRefField = type == ModuleResourceType.Utm ? "ResRef" : "TemplateResRef";
        root.SetString(resRefField, GffFieldType.ResRef, resRef);
        root.SetString("Tag", GffFieldType.CExoString, "template_tag");
        root.SetString("BlueprintMarker", GffFieldType.CExoString, marker);
        if (type == ModuleResourceType.Utm)
            root.Add("StoreList", JsonGffField.CreateList());
        return new JsonGffDocument("GFF ", root);
    }

    private static JsonGffField TransformField(float scale)
    {
        var transform = JsonGffField.CreateStruct(0).Struct!;
        transform.SetSingle("ScaleX", scale);
        transform.SetSingle("ScaleY", scale + 1);
        var field = JsonGffField.CreateStruct(0);
        foreach (var child in transform.Entries)
            field.Struct!.Add(child.Key, child.Value);
        return field;
    }

    private static JsonGffField TransformListField(float scale)
    {
        var component = JsonGffField.CreateStruct(0).Struct!;
        component.SetString("Name", GffFieldType.CExoString, "ScaleX");
        component.SetSingle("ValueTo", scale);
        var list = JsonGffField.CreateList();
        list.InsertElement(0, component);
        return list;
    }

    private static JsonGffStruct FieldAsStruct(JsonGffField field)
    {
        var root = new JsonGffStruct();
        root.Add("Value", InstanceFieldMap.CloneField(field));
        return root;
    }
}
