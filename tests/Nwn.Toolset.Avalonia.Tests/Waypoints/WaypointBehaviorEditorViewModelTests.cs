using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Editing;
using Nwn.Toolset.Avalonia.Tests.Support;
using Nwn.Toolset.Avalonia.Waypoints;

namespace Nwn.Toolset.Avalonia.Tests.Waypoints;

[TestClass]
public sealed class WaypointBehaviorEditorViewModelTests
{
    [TestMethod]
    public async Task ChoosingAPersistedBehaviorRecordsItAndLeavingItRemovesTheRecord()
    {
        var (session, waypoint) = Waypoint();
        var editor = Editor(session, waypoint);
        Assert.AreEqual("custom", editor.Behavior.Id);
        Assert.IsNotNull(editor.Variables);

        await editor.ChooseBehaviorAsync(FakeWaypointBehaviorCatalog.Destination);
        Assert.AreEqual("destination", new VarTable(waypoint).GetString(FakeWaypointBehaviorCatalog.Local));
        Assert.AreEqual("Destination still needs Destination tag.", editor.Incomplete);

        await editor.ChooseBehaviorAsync(FakeWaypointBehaviorCatalog.MapNote);
        Assert.IsNull(new VarTable(waypoint).GetString(FakeWaypointBehaviorCatalog.Local));
        Assert.AreEqual(1, waypoint.GetIntOrNull("HasMapNote"));
        Assert.IsFalse(editor.NeedsSaveNormalization);
    }

    [TestMethod]
    public void SaveNormalizationReappliesManagedValuesAndRefusesAConflictingSingletonTag()
    {
        var (session, waypoint) = Waypoint();
        session.Execute("seed", () =>
        {
            new VarTable(waypoint).SetString(FakeWaypointBehaviorCatalog.Local, "destination");
            waypoint.SetString("Tag", GffFieldType.CExoString, "UNIQUE_DESTINATION");
        });
        var inUse = false;
        var editor = Editor(session, waypoint, _ => inUse);
        Assert.AreEqual("destination", editor.Behavior.Id);
        Assert.IsTrue(editor.PrepareForSave());

        inUse = true;
        Assert.IsFalse(editor.PrepareForSave());
        Assert.AreEqual(
            "This destination tag is already used by another placed waypoint. Choose a unique destination.",
            editor.Incomplete);

        var catalog = new FakeWaypointBehaviorCatalog();
        editor.RefreshCatalog(catalog);
        Assert.AreEqual("destination", editor.Behavior.Id);
        Assert.AreEqual("instance", Editor(session, waypoint, isInstance: true).HeaderKind);
    }

    private static WaypointBehaviorEditorViewModel Editor(
        DocumentSession session, JsonGffStruct waypoint, Func<string, bool>? inUse = null, bool isInstance = false) =>
        new(waypoint, "wp_test", isInstance, (description, mutation) =>
        {
            session.Execute(description, mutation);
            return true;
        }, new WaypointBehaviorEditorHost { Catalog = new FakeWaypointBehaviorCatalog() }, inUse);

    private static (DocumentSession Session, JsonGffStruct Waypoint) Waypoint()
    {
        var document = NativeTestDocuments.Create("UTW ");
        return (new DocumentSession("wp.utw", document), document.Root);
    }
}
