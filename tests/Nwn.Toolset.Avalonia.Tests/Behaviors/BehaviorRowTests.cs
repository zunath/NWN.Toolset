using System.Text;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Nwn.Authoring.Behaviors;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Toolset.Avalonia.Behaviors;
using Nwn.Toolset.Avalonia.Localization;
using Nwn.Toolset.Avalonia.Tests.Support;

namespace Nwn.Toolset.Avalonia.Tests.Behaviors;

[TestClass]
public sealed class BehaviorRowTests
{
    [TestMethod]
    public async Task ExtractedTextRowUsesTheHostTransactionAndRetainsItsNativeStorageShape()
    {
        var native = JsonGffDocument.Parse(Encoding.UTF8.GetBytes("""
            {"__data_type":"UTD ","LinkedTo":{"type":"cexostring","value":"first"},
            "FutureField":{"type":"int","value":7}}
            """));
        var descriptions = new List<string>();
        using var row = new BehaviorRowViewModel(new()
        {
            Label = "Destination", Name = "LinkedTo", Kind = BehaviorFieldKind.TagReference,
            FieldType = GffFieldType.CExoString,
        }, new(native.Root), (description, mutate) =>
        {
            descriptions.Add(description);
            mutate();
            return true;
        });
        row.Reload();
        await GraphTestRuntime.RunAsync(() => new BehaviorRowView { DataContext = row }, window =>
        {
            var input = ((BehaviorRowView)window.Content!).GetVisualDescendants().OfType<TextBox>()
                .First(box => !box.AcceptsReturn);
            Assert.AreEqual("first", input.Text);
            input.Text = "second";
            Assert.AreEqual("second", new BehaviorValueStore(native.Root).GetString(BehaviorFieldStorage.Field, "LinkedTo"));
            Assert.AreEqual("Change Destination", descriptions.Single());
            Assert.AreEqual(GffFieldType.CExoString, native.Root.GetOrNull("LinkedTo")!.Type);
            Assert.AreEqual(7L, native.Root.GetOrNull("FutureField")!.GetInteger());
        });
    }

    [TestMethod]
    public void PropertyRowCaptionsAndSummariesComeFromTheReplaceableTypedCatalog()
    {
        var entries = Enum.GetValues<BehaviorRowStringId>().ToDictionary(id => id,
            id => BehaviorRowTexts.English.Get(id, 1, 2));
        entries[BehaviorRowStringId.Choose] = "Select a destination";
        entries[BehaviorRowStringId.Options] = "Available: {0}";
        using var row = new BehaviorRowViewModel(new()
        {
            Label = "Destination", Name = "LinkedToFlags", Kind = BehaviorFieldKind.Choice,
            Choices = [new(1, "Door"), new(2, "Waypoint")],
        }, new(JsonGffDocument.Parse(Encoding.UTF8.GetBytes("""{"__data_type":"UTD "}""")).Root),
            (_, mutate) => { mutate(); return true; }, texts: new(entries));
        Assert.AreEqual("Select a destination", row.ChooseLabel);
        Assert.AreEqual("Available: 2", row.SearchSummary);
    }
}
