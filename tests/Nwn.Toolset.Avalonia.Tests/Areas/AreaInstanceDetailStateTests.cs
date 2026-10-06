using System.Text;
using Nwn.Authoring.Areas.Placement;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Resources;
using Nwn.Toolset.Avalonia.Areas;

namespace Nwn.Toolset.Avalonia.Tests.Areas;

[TestClass]
public sealed class AreaInstanceDetailStateTests
{
    private static readonly AreaInstanceDetailLabels Labels = new(
        "Tag", "X", "Y", "Z", "Facing X", "Facing Y", "Width", "Height");

    private static readonly AreaInstanceDetailEditDescriptions Descriptions = new(
        "Change placed tag", "Move placement", "Turn placement", "Resize trigger");

    [TestMethod]
    public void DetailEditsUseHostTransactionsAndPreserveUnknownFields()
    {
        var instance = CreateInstance(ModuleResourceType.Utc, "first", "retained");
        var descriptions = new List<string>();
        var state = new AreaInstanceDetailState(
            ModuleResourceType.Utc,
            (description, edit) =>
            {
                descriptions.Add(description);
                edit();
                return true;
            },
            Labels,
            Descriptions);

        state.SetInstance(instance);
        state.DetailTag = "updated";
        state.DetailX = 12.5;

        Assert.AreEqual("updated", InstanceFieldMap.GetTag(instance));
        Assert.AreEqual(12.5f, InstanceFieldMap.GetPosition(ModuleResourceType.Utc, instance).X);
        Assert.AreEqual("retained", instance.Get("FutureField").GetString());
        Assert.AreEqual("updated", state.DetailTag);
        CollectionAssert.AreEqual(new[] { "Change placed tag", "Move placement" }, descriptions);
    }

    [TestMethod]
    public void RefusedAndNonFiniteEditsRestoreValuesFromTheNativeInstance()
    {
        var instance = CreateInstance(ModuleResourceType.Utc, "first", "retained");
        var editCount = 0;
        var state = new AreaInstanceDetailState(
            ModuleResourceType.Utc,
            (_, _) => { editCount++; return false; },
            Labels,
            Descriptions);
        state.SetInstance(instance);

        state.DetailX = 99;
        state.DetailY = double.MaxValue;

        Assert.AreEqual(1, editCount, "A value that cannot be represented as a native float must not reach the host transaction.");
        Assert.AreEqual(0d, state.DetailX);
        Assert.AreEqual(0d, state.DetailY);
        Assert.AreEqual(0f, InstanceFieldMap.GetPosition(ModuleResourceType.Utc, instance).X);

        var trigger = CreateInstance(ModuleResourceType.Utt, "trigger", "future");
        var triggerState = new AreaInstanceDetailState(
            ModuleResourceType.Utt,
            (_, _) => false,
            Labels,
            Descriptions);
        triggerState.SetInstance(trigger);
        triggerState.DetailTriggerWidth = 8;

        Assert.AreEqual(2d, triggerState.DetailTriggerWidth);
        Assert.AreEqual((2f, 2f), InstanceFieldMap.GetTriggerGeometrySize(trigger));
        triggerState.DetailTriggerWidth = 0;
        Assert.AreEqual(2d, triggerState.DetailTriggerWidth,
            "An invalid dimension must restore when the selected native footprint is valid.");
        triggerState.DetailTriggerWidth = -1;
        Assert.AreEqual(2d, triggerState.DetailTriggerWidth);

        var malformed = CreateInstance(ModuleResourceType.Utt, "malformed", "future");
        malformed.Remove("Geometry");
        var repairState = new AreaInstanceDetailState(
            ModuleResourceType.Utt,
            (_, edit) => { edit(); return true; },
            Labels,
            Descriptions);
        repairState.SetInstance(malformed);
        repairState.DetailTriggerWidth = 4;
        Assert.AreEqual(4d, repairState.DetailTriggerWidth,
            "The first valid dimension remains as an uncommitted input until both dimensions are valid.");
        repairState.DetailTriggerHeight = 6;

        Assert.AreEqual((4f, 6f), InstanceFieldMap.GetTriggerGeometrySize(malformed));
    }

    [TestMethod]
    public void DeferredEditCannotMutateAReplacementSelection()
    {
        var first = CreateInstance(ModuleResourceType.Utc, "first", "one");
        var second = CreateInstance(ModuleResourceType.Utc, "second", "two");
        Action? deferredMutation = null;
        var state = new AreaInstanceDetailState(
            ModuleResourceType.Utc,
            (_, edit) => { deferredMutation = edit; return true; },
            Labels,
            Descriptions);
        state.SetInstance(first);

        state.DetailTag = "stale edit";
        state.SetInstance(second);
        deferredMutation!();

        Assert.AreEqual("first", InstanceFieldMap.GetTag(first));
        Assert.AreEqual("second", InstanceFieldMap.GetTag(second));
        Assert.AreEqual("second", state.DetailTag);
    }

    [TestMethod]
    public void DeferredEditCannotMutateTheSameSelectionAfterTransactionReturns()
    {
        var instance = CreateInstance(ModuleResourceType.Utc, "original", "future");
        Action? deferredMutation = null;
        var state = new AreaInstanceDetailState(
            ModuleResourceType.Utc,
            (_, edit) =>
            {
                deferredMutation = edit;
                return true;
            },
            Labels,
            Descriptions);
        state.SetInstance(instance);

        state.DetailTag = "late mutation";
        Assert.AreEqual("original", state.DetailTag,
            "A callback not invoked during the host transaction is not an accepted edit.");
        deferredMutation!();

        Assert.AreEqual("original", InstanceFieldMap.GetTag(instance));
        Assert.AreEqual("original", state.DetailTag);
    }

    private static JsonGffStruct CreateInstance(ModuleResourceType type, string tag, string futureValue)
    {
        var typeCode = type == ModuleResourceType.Utt ? "UTT " : "UTC ";
        var blueprint = JsonGffDocument.Parse(Encoding.UTF8.GetBytes($$$"""
            {"__data_type":"{{{typeCode}}}","Tag":{"type":"cexostring","value":"{{{tag}}}"},
            "TemplateResRef":{"type":"resref","value":"fixture"},
            "FutureField":{"type":"cexostring","value":"{{{futureValue}}}"}}
            """));
        return InstanceFieldMap.CreateInstance(type, blueprint, "fixture", 0, 0, 0);
    }
}
