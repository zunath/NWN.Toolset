using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Resources;
using Nwn.Toolset.Avalonia.Areas;
using Nwn.Toolset.Avalonia.Areas.Properties;
using Nwn.Toolset.Avalonia.Areas.Properties.Views;
using Nwn.Toolset.Avalonia.Doors;
using Nwn.Toolset.Avalonia.Doors.Views;
using Nwn.Toolset.Avalonia.Fields;
using Nwn.Toolset.Avalonia.Fields.Views;
using Nwn.Toolset.Avalonia.Tests.Support;
using Nwn.Toolset.Avalonia.Triggers;
using Nwn.Toolset.Avalonia.Triggers.Views;

namespace Nwn.Toolset.Avalonia.Tests.Areas.Properties;

[TestClass]
public sealed class AreaPropertiesPageViewTests
{
    [TestMethod]
    public Task ASelectedTriggerIsDrawnByTheTypedTriggerEditorBesideTheGeometryForm()
    {
        var documents = NativeTestDocuments.CreateArea();
        documents.ExecuteInstances("seed", () =>
        {
            var list = JsonGffField.CreateList();
            documents.Instances.Document.Root.Add("TriggerList", list);
            list.InsertElement(0, NativeTestDocuments.Placement("trigger_a", "trigger_tpl"));
            new GicDocument(documents.Comments.Document).InsertBlankComment("TriggerList", ModuleResourceType.Utt, 0, 1);
        });
        var section = new AreaInstanceSectionViewModel("Triggers", "TriggerList", ModuleResourceType.Utt,
            new AreaInstanceSectionHost
            {
                Instances = documents.Instances,
                Comments = documents.Comments,
                RunEdit = documents.ExecuteInstances,
                Blueprints = new FakeAreaInstanceBlueprintSource(),
                Editors = new AreaInstanceEditorFactory(
                    "test_area", triggers: new TriggerBehaviorEditorHost { Catalog = new FakeTriggerBehaviorCatalog() }),
            })
        { IsExpanded = true };
        var page = new AreaPropertiesPageViewModel(new EditorFieldContext(documents.Area.Document, documents.ExecuteArea));
        page.Sections.Add(section);

        return GraphTestRuntime.RunAsync(
            () => new ScrollViewer { Content = new AreaPropertiesPageView { DataContext = page } },
            window =>
            {
                try
                {
                    var view = window.GetVisualDescendants().OfType<AreaPropertiesPageView>().Single();
                    var grid = view.GetVisualDescendants().OfType<DataGrid>().Single(candidate => candidate.DataContext == section);
                    grid.SelectedItem = section.Rows.Single();
                    Dispatcher.UIThread.RunJobs();
                    window.UpdateLayout();

                    var triggerView = view.GetVisualDescendants().OfType<TriggerBehaviorEditorView>()
                        .Single(candidate => candidate.IsEffectivelyVisible);
                    Assert.AreSame(section.TriggerEditor, triggerView.DataContext);
                    Assert.IsTrue(view.GetVisualDescendants().OfType<AreaInstanceDetailForm>()
                        .Any(form => form.DataContext == section && form.IsEffectivelyVisible));
                }
                finally
                {
                    section.Dispose();
                    documents.Dispose();
                }
            });
    }

    [TestMethod]
    public Task ThePageShowsAreaGroupsAndOneSectionPerInstanceListWithItsGridAndTypedEditor()
    {
        var documents = NativeTestDocuments.CreateArea(NativeTestDocuments.Parse(
            """{"__data_type":"ARE ","Tag":{"type":"cexostring","value":"area_tag"},"WindPower":{"type":"int","value":2}}"""));
        documents.ExecuteInstances("seed", () =>
        {
            var list = JsonGffField.CreateList();
            documents.Instances.Document.Root.Add("Door List", list);
            list.InsertElement(0, NativeTestDocuments.Placement("door_a", "door_tpl"));
            new GicDocument(documents.Comments.Document).InsertBlankComment("Door List", ModuleResourceType.Utd, 0, 1);
        });
        var page = new AreaPropertiesPageViewModel(
            new EditorFieldContext(documents.Area.Document, documents.ExecuteArea)) { AreaPropertiesExpanded = true };
        var editors = new AreaInstanceEditorFactory(
            "test_area", new DoorBehaviorEditorHost { Catalog = new FakeDoorBehaviorCatalog() });
        var palettes = new FakeAreaInstancePaletteSource();
        palettes.Palettes[ModuleResourceType.Utd] = [];
        foreach (var definition in AreaInstanceSectionCatalog.All)
        {
            page.Sections.Add(new AreaInstanceSectionViewModel(
                page.SectionTitle(definition), definition.ListFieldName, definition.Type, new AreaInstanceSectionHost
                {
                    Instances = documents.Instances,
                    Comments = documents.Comments,
                    RunEdit = documents.ExecuteInstances,
                    Blueprints = new FakeAreaInstanceBlueprintSource(),
                    Palettes = palettes,
                    Editors = editors,
                }));
        }

        var doors = page.SectionFor(ModuleResourceType.Utd)!;
        doors.IsExpanded = true;

        return GraphTestRuntime.RunAsync(
            () => new ScrollViewer { Content = new AreaPropertiesPageView { DataContext = page } },
            window =>
            {
                try
                {
                    var view = window.GetVisualDescendants().OfType<AreaPropertiesPageView>().Single();
                    var headers = view.GetVisualDescendants().OfType<Expander>()
                        .Select(expander => expander.Header as string)
                        .Where(header => header != null)
                        .ToList();
                    CollectionAssert.IsSubsetOf(
                        new[] { "Area Properties", "Identity", "Flags", "Lighting", "Weather", "Loading" }, headers);
                    var titles = view.GetVisualDescendants().OfType<TextBlock>().Select(text => text.Text).ToList();
                    CollectionAssert.IsSubsetOf(
                        new[] { "Creatures", "Placeables", "Doors", "Waypoints", "Stores", "Sounds", "Triggers", "Items" },
                        titles);

                    var fields = view.GetVisualDescendants().OfType<FieldView>().ToList();
                    Assert.AreEqual(page.AreaPropertyGroups.Sum(group => group.Fields.Count), fields.Count);
                    Assert.IsTrue(view.GetVisualDescendants().OfType<TextBox>().Any(box => box.Text == "area_tag"),
                        "The Tag field is drawn by the shared text-field template.");

                    var grid = view.GetVisualDescendants().OfType<DataGrid>().Single(candidate => candidate.DataContext == doors);
                    CollectionAssert.AreEqual(new object[] { "Tag", "Template", "X", "Y", "Z" },
                        grid.Columns.Select(column => column.Header).ToArray());
                    var buttons = view.GetVisualDescendants().OfType<Button>()
                        .Where(button => button.DataContext == doors).ToList();
                    CollectionAssert.IsSubsetOf(new object[] { "Add...", "Duplicate", "Delete" },
                        buttons.Select(button => button.Content).ToArray());
                    Assert.IsFalse(buttons.Single(button => Equals(button.Content, "Delete")).IsEffectivelyEnabled);

                    grid.SelectedItem = doors.Rows.Single();
                    Dispatcher.UIThread.RunJobs();
                    window.UpdateLayout();
                    Assert.AreSame(doors.Rows.Single(), doors.SelectedRow);
                    var doorView = view.GetVisualDescendants().OfType<DoorBehaviorEditorView>().Single(candidate => candidate.IsEffectivelyVisible);
                    Assert.AreSame(doors.DoorEditor, doorView.DataContext);
                    Assert.IsTrue(view.GetVisualDescendants().OfType<AreaInstanceDetailForm>()
                        .Any(form => form.DataContext == doors && form.IsEffectivelyVisible));

                    doors.AddCommand.Execute(null);
                    Dispatcher.UIThread.RunJobs();
                    window.UpdateLayout();
                    Assert.IsTrue(view.GetVisualDescendants().OfType<TextBlock>()
                        .Any(text => text.Text == "Choose Doors blueprint" && text.IsEffectivelyVisible));
                    Assert.IsTrue(view.BringSectionIntoView(doors));
                }
                finally
                {
                    foreach (var section in page.Sections)
                        section.Dispose();
                    documents.Dispose();
                }
            });
    }
}
