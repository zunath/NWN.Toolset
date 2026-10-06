using System.Numerics;
using System.Text;
using Nwn.Authoring.Areas.Editing;
using Nwn.Authoring.Areas.Placement;
using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Resources;
using Nwn.Preview.Areas;
using Nwn.Toolset.Avalonia.Areas.Properties;
using Nwn.Toolset.Avalonia.Doors;
using Nwn.Toolset.Avalonia.Sounds;
using Nwn.Toolset.Avalonia.Tests.Support;
using Nwn.Toolset.Avalonia.Triggers;
using Nwn.Toolset.Avalonia.Waypoints;

namespace Nwn.Toolset.Avalonia.Tests.Areas.Properties;

[TestClass]
public sealed class AreaInstanceSectionViewModelTests
{
    [TestMethod]
    public void AddFromThePaletteDuplicateAndDeleteKeepTheGitAndGicListsAligned()
    {
        using var documents = NativeTestDocuments.CreateArea();
        var blueprints = new FakeAreaInstanceBlueprintSource();
        blueprints.Blueprints["crate"] = NativeTestDocuments.Parse(
            """{"__data_type":"UTP ","Tag":{"type":"cexostring","value":"crate_tag"},"FutureField":{"type":"int","value":7}}""");
        var palettes = new FakeAreaInstancePaletteSource();
        palettes.Palettes[ModuleResourceType.Utp] = ItpDocument.Parse(Encoding.UTF8.GetBytes(
            """
            {"__data_type":"ITP ","MAIN":{"type":"list","value":[{"__struct_id":0,
              "NAME":{"type":"cexostring","value":"Containers"},
              "LIST":{"type":"list","value":[{"__struct_id":0,
                "NAME":{"type":"cexostring","value":"Crate"},
                "RESREF":{"type":"resref","value":"crate"}}]}}]}}
            """)).Nodes;
        var descriptions = new List<string>();
        var section = Section(documents, ModuleResourceType.Utp, "Placeable List", "Placeables", blueprints, palettes,
            descriptions: descriptions);

        section.AddCommand.Execute(null);
        var browser = section.ActivePaletteBrowser!;
        Assert.AreEqual("Choose Placeables blueprint", browser.Heading);
        browser.SelectedNode = browser.Nodes.Single();
        browser.ChooseCommand.Execute(null);
        Assert.AreEqual("Select a blueprint (leaf) node first.", browser.StatusMessage);
        browser.SelectedNode = browser.Nodes.Single().Children.Single();
        browser.ChooseCommand.Execute(null);

        Assert.IsNull(section.ActivePaletteBrowser);
        Assert.AreEqual(1, section.Rows.Count);
        Assert.AreEqual("crate_tag", section.Rows[0].Tag);
        Assert.AreEqual("crate", section.Rows[0].TemplateResRef);
        Assert.AreEqual(7L, Instances(documents, "Placeable List")[0].Get("FutureField").GetInteger());
        Assert.AreEqual(1, Comments(documents, "Placeable List").Count);

        section.SelectedRow = section.Rows[0];
        section.DuplicateCommand.Execute(null);
        Assert.AreEqual(2, section.Rows.Count);
        Assert.AreEqual(2, Comments(documents, "Placeable List").Count);

        Assert.IsTrue(section.DeleteInstances([0, 1]));
        Assert.AreEqual(0, section.Rows.Count);
        Assert.AreEqual(0, Comments(documents, "Placeable List").Count);
        CollectionAssert.AreEqual(
            new[] { "Add Placeables instance", "Duplicate Placeables instance", "Delete 2 Placeables instances" },
            descriptions);

        Assert.IsTrue(documents.UndoInstances());
        Assert.AreEqual(2, Instances(documents, "Placeable List").Count);
        section.RefreshFromDocument();
        Assert.AreEqual(2, section.Rows.Count);
    }

    [TestMethod]
    public void MissingAndUnreadablePalettesAreReportedWithoutOpeningAPlacement()
    {
        using var documents = NativeTestDocuments.CreateArea();
        var palettes = new FakeAreaInstancePaletteSource();
        var log = new FakePaletteLog();
        var section = Section(documents, ModuleResourceType.Utc, "Creature List", "Creatures",
            new FakeAreaInstanceBlueprintSource(), palettes, log: log);

        section.AddCommand.Execute(null);
        Assert.IsNull(section.ActivePaletteBrowser);
        Assert.AreEqual("No palette file found for Creatures ('palette/Utc').", log.Lines.Single());

        palettes.Palettes[ModuleResourceType.Utc] = [];
        palettes.ReadFailure = new InvalidDataException("bad palette");
        section.AddCommand.Execute(null);
        Assert.AreEqual("This palette could not be read: bad palette", section.ActivePaletteBrowser!.StatusMessage);
        Assert.AreEqual("Failed to read palette 'palette/Utc': bad palette", log.Lines[1]);
        section.ActivePaletteBrowser.CancelCommand.Execute(null);
        Assert.IsNull(section.ActivePaletteBrowser);

        Assert.IsFalse(section.AddInstanceAt("absent", 1, 2, 3));
        StringAssert.StartsWith(log.Lines[2], "Failed to add Creatures instance 'absent':");
    }

    [TestMethod]
    public void SelectingAPlacementChoosesTheHostsTypedEditorOrTheLocalVariables()
    {
        using var documents = NativeTestDocuments.CreateArea();
        AddInstance(documents, "Door List", ModuleResourceType.Utd, NativeTestDocuments.Placement("door_a", "door"));
        AddInstance(documents, "WaypointList", ModuleResourceType.Utw, NativeTestDocuments.Placement("wp_a", "wp"));
        AddInstance(documents, "SoundList", ModuleResourceType.Uts, NativeTestDocuments.Placement("snd_a", "snd"));
        AddInstance(documents, "Creature List", ModuleResourceType.Utc, NativeTestDocuments.Placement("npc_a", "npc"));
        var editors = new AreaInstanceEditorFactory(
            "test_area",
            new DoorBehaviorEditorHost { Catalog = new FakeDoorBehaviorCatalog() },
            new WaypointBehaviorEditorHost { Catalog = new FakeWaypointBehaviorCatalog() },
            new SoundBehaviorEditorHost { Catalog = new FakeSoundBehaviorCatalog() });

        var doors = Section(documents, ModuleResourceType.Utd, "Door List", "Doors", editors: editors);
        doors.SelectedRow = doors.Rows.Single();
        Assert.IsTrue(doors.UsesDoorEditor);
        Assert.IsNotNull(doors.DoorEditor);
        Assert.AreEqual("instance", doors.DoorEditor.HeaderKind);
        Assert.AreEqual("test_area", doors.DoorEditor.HeaderOwner);
        Assert.IsNull(doors.VarTableSection);
        Assert.IsFalse(doors.UsesGenericDetailEditor);
        doors.DoorEditor.BasicRows.Single(row => row.Definition.Name == "Tag").Text = "door_b";
        Assert.AreEqual("door_b", doors.Rows.Single().Tag, "A typed-editor edit updates the grid row in place.");

        var waypoints = Section(documents, ModuleResourceType.Utw, "WaypointList", "Waypoints", editors: editors);
        waypoints.SelectedRow = waypoints.Rows.Single();
        Assert.IsTrue(waypoints.HasWaypointBehaviorEditor);
        Assert.IsNull(waypoints.VarTableSection);

        var sounds = Section(documents, ModuleResourceType.Uts, "SoundList", "Sounds", editors: editors);
        sounds.SelectedRow = sounds.Rows.Single();
        Assert.IsTrue(sounds.HasSoundBehaviorEditor);
        sounds.SoundEditor!.BasicRows.Single(row => row.Definition.Name == "Tag").Text = "snd_b";
        Assert.AreEqual("snd_b", sounds.Rows.Single().Tag);

        var creatures = Section(documents, ModuleResourceType.Utc, "Creature List", "Creatures", editors: editors);
        creatures.SelectedRow = creatures.Rows.Single();
        Assert.IsNotNull(creatures.VarTableSection);
        Assert.IsTrue(creatures.UsesGenericDetailEditor);

        var plain = Section(documents, ModuleResourceType.Utd, "Door List", "Doors");
        plain.SelectedRow = plain.Rows.Single();
        Assert.IsFalse(plain.UsesDoorEditor);
        Assert.IsNotNull(plain.VarTableSection, "Without an editor factory a door keeps the generic form and locals.");

        doors.SelectedRow = null;
        Assert.IsNull(doors.DoorEditor);
        Assert.IsFalse(doors.HasSelection);
    }

    [TestMethod]
    public void ASelectedTriggerGetsTheTypedEditorBesideTheGeometryForm()
    {
        using var documents = NativeTestDocuments.CreateArea();
        AddInstance(documents, "TriggerList", ModuleResourceType.Utt, NativeTestDocuments.Placement("trigger_a", "trigger"));
        var editors = new AreaInstanceEditorFactory(
            "test_area", triggers: new TriggerBehaviorEditorHost { Catalog = new FakeTriggerBehaviorCatalog() });

        var triggers = Section(documents, ModuleResourceType.Utt, "TriggerList", "Triggers", editors: editors);
        triggers.SelectedRow = triggers.Rows.Single();
        Assert.IsTrue(triggers.HasTriggerBehaviorEditor);
        Assert.AreEqual("instance", triggers.TriggerEditor!.HeaderKind);
        Assert.AreEqual("test_area", triggers.TriggerEditor.HeaderOwner);
        Assert.IsNull(triggers.VarTableSection, "The raw behavior owns the locals, not the section.");
        Assert.IsFalse(triggers.UsesGenericDetailEditor, "The typed editor owns the tag.");
        Assert.IsTrue(triggers.HasTriggerGeometry, "The area's form keeps the trigger's geometry.");
        triggers.TriggerEditor.BasicRows.Single(row => row.Definition.Name == "Tag").Text = "trigger_b";
        Assert.AreEqual("trigger_b", triggers.Rows.Single().Tag, "A typed-editor edit updates the grid row in place.");

        var plain = Section(documents, ModuleResourceType.Utt, "TriggerList", "Triggers",
            editors: new AreaInstanceEditorFactory("test_area"));
        plain.SelectedRow = plain.Rows.Single();
        Assert.IsFalse(plain.HasTriggerBehaviorEditor);
        Assert.IsTrue(plain.UsesGenericDetailEditor, "Without trigger data a trigger keeps the generic form.");
        Assert.IsNotNull(plain.VarTableSection);

        triggers.SelectedRow = null;
        Assert.IsNull(triggers.TriggerEditor);
    }

    [TestMethod]
    public void SingletonDestinationTagsRefuseASaveThatWouldMakeThemAmbiguous()
    {
        using var documents = NativeTestDocuments.CreateArea();
        AddInstance(documents, "WaypointList", ModuleResourceType.Utw, NativeTestDocuments.Placement("UNIQUE_DESTINATION", "wp"));
        AddInstance(documents, "WaypointList", ModuleResourceType.Utw, NativeTestDocuments.Placement("ordinary", "wp"));
        var policy = new FakeAreaWaypointTagPolicy();
        policy.Singletons.Add("UNIQUE_DESTINATION");
        var section = Section(documents, ModuleResourceType.Utw, "WaypointList", "Waypoints", waypointTags: policy);

        Assert.IsTrue(section.PrepareForSave());
        policy.OutsideArea["UNIQUE_DESTINATION"] = 1;
        Assert.IsFalse(section.PrepareForSave());
        policy.OutsideArea.Clear();
        Assert.IsTrue(section.DeleteInstances([1]));
        AddInstance(documents, "WaypointList", ModuleResourceType.Utw, NativeTestDocuments.Placement("unique_destination", "wp"));
        Assert.IsFalse(section.PrepareForSave(), "Tags compare without case, as the engine does.");
    }

    [TestMethod]
    public void CopiesPasteOnlyIntoTheSameModuleAndKeepTheirComment()
    {
        using var documents = NativeTestDocuments.CreateArea();
        AddInstance(documents, "Creature List", ModuleResourceType.Utc, NativeTestDocuments.Placement("npc_a", "npc"));
        var section = Section(documents, ModuleResourceType.Utc, "Creature List", "Creatures");
        var marker = new InstanceMarker { Kind = InstanceMarkerKind.Creature, Position = Vector3.Zero, Orientation = Vector2.UnitX };

        var copy = section.CopyInstanceForPlacement(0, marker)!;
        Assert.AreEqual("module-a", copy.ModuleRoot);
        Assert.IsTrue(section.AddCopiedInstanceAt(copy, 4, 5, 6, 0, 1));
        Assert.AreEqual(2, section.Rows.Count);
        Assert.AreEqual(4f, section.Rows[1].X);
        Assert.AreEqual(2, Comments(documents, "Creature List").Count);

        var foreign = copy with { ModuleRoot = "module-b" };
        Assert.IsFalse(section.AddCopiedInstanceAt(foreign, 0, 0, 0, 1, 0));
        Assert.IsTrue(section.SetInstanceTransform(1, 7, 8, 9, 0, 1));
        Assert.AreEqual((7f, 8f, 9f), InstanceFieldMap.GetPosition(ModuleResourceType.Utc, section.GetInstanceForScene(1)!));
    }

    private static AreaInstanceSectionViewModel Section(
        AreaDocumentEditSession documents,
        ModuleResourceType type,
        string listField,
        string title,
        IAreaInstanceBlueprintSource? blueprints = null,
        IAreaInstancePaletteSource? palettes = null,
        IAreaInstanceEditorFactory? editors = null,
        IAreaWaypointTagPolicy? waypointTags = null,
        FakePaletteLog? log = null,
        List<string>? descriptions = null) =>
        new(title, listField, type, new AreaInstanceSectionHost
        {
            Instances = documents.Instances,
            Comments = documents.Comments,
            RunEdit = (description, mutation) =>
            {
                descriptions?.Add(description);
                return documents.ExecuteInstances(description, mutation);
            },
            Blueprints = blueprints ?? new FakeAreaInstanceBlueprintSource(),
            Palettes = palettes,
            Editors = editors,
            WaypointTags = waypointTags,
            Log = log,
        });

    private static void AddInstance(
        AreaDocumentEditSession documents, string listField, ModuleResourceType type, JsonGffStruct instance)
    {
        documents.ExecuteInstances("seed", () =>
        {
            var list = documents.Instances.Document.Root.GetOrNull(listField);
            if (list == null)
            {
                list = JsonGffField.CreateList();
                documents.Instances.Document.Root.Add(listField, list);
            }

            list.InsertElement(list.Elements!.Count, instance);
            new GicDocument(documents.Comments.Document).InsertBlankComment(
                listField, type, list.Elements.Count - 1, list.Elements.Count);
        });
    }

    private static IReadOnlyList<JsonGffStruct> Instances(AreaDocumentEditSession documents, string listField) =>
        documents.Instances.Document.Root.GetOrNull(listField)?.Elements ?? (IReadOnlyList<JsonGffStruct>)[];

    private static IReadOnlyList<JsonGffStruct> Comments(AreaDocumentEditSession documents, string listField) =>
        documents.Comments.Document.Root.GetOrNull(listField)?.Elements ?? (IReadOnlyList<JsonGffStruct>)[];
}
