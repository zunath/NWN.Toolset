using System.Text;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Toolset.Avalonia.Tests.Support;
using Nwn.Toolset.Avalonia.Variables;

namespace Nwn.Toolset.Avalonia.Tests.Variables;

[TestClass]
public sealed class VarTableSectionViewTests
{
    [TestMethod]
    public async Task SharedEditorUsesOwnerTransactionAndRetainsUnknownNativeDataAcrossReopen()
    {
        var document = JsonGffDocument.Parse(Encoding.UTF8.GetBytes("""
            {"__data_type":"GIT ","FutureField":{"type":"int","value":19}}
            """));
        var table = new VarTable(document.Root);
        table.SetString("FUTURE_LOCAL", "retained");
        var descriptions = new List<string>();
        var section = new VarTableSectionViewModel((description, mutate) =>
        {
            descriptions.Add(description);
            mutate();
            return true;
        }, table, ["HOST_KEY"]);
        Assert.AreEqual("string", section.Rows.Single().TypeLabel);
        await GraphTestRuntime.RunAsync(() => new VarTableSectionView { DataContext = section }, window =>
        {
            var grid = window.GetVisualDescendants().OfType<DataGrid>().Single();
            Assert.AreSame(section.Rows, grid.ItemsSource);
            window.UpdateLayout();
            Assert.IsTrue(grid.GetVisualDescendants().OfType<DataGridRow>().Any(), "The native variable rows must actually render.");
            grid.SelectedItem = section.Rows.Single();
            Assert.AreSame(section.Rows.Single(), section.SelectedRow, "Grid selection must enable editing/removing the selected native local.");
            window.Width = 330;
            window.UpdateLayout();
            var name = window.GetVisualDescendants().OfType<AutoCompleteBox>().Single();
            var value = window.GetVisualDescendants().OfType<TextBox>().Single(box => box.Watermark?.ToString() == section.ValueLabel);
            Assert.IsTrue(name.Bounds.Width >= 80);
            Assert.IsTrue(value.Bounds.Width >= 80);
            Assert.IsTrue(window.GetVisualDescendants().OfType<Button>().Any(button => button.Content?.ToString() == section.SetLabel));
            section.NewName = "HOST_KEY";
            section.NewType = "string";
            section.NewValue = "ready";
            section.SetVariableCommand.Execute(null);
        });
        Assert.AreEqual("Set local HOST_KEY", descriptions.Single());

        var reopened = JsonGffDocument.Parse(document.ToBytes());
        var locals = new VarTable(reopened.Root).ToList();
        Assert.AreEqual("ready", locals.Single(entry => entry.Name == "HOST_KEY").StringValue);
        Assert.AreEqual(19L, reopened.Root.GetOrNull("FutureField")!.GetInteger());
        Assert.AreEqual("retained", locals.Single(entry => entry.Name == "FUTURE_LOCAL").StringValue);
    }
}
