using Avalonia.Controls;
using Nwn.Toolset.Avalonia.Areas;
using Nwn.Toolset.Avalonia.Tests.Support;

namespace Nwn.Toolset.Avalonia.Tests.Areas;

[TestClass]
public sealed class AreaCreationFormTests
{
    [TestMethod]
    public async Task BindsHostFieldsChoicesLabelsAndCommands()
    {
        var state = new AreaCreationFormState();
        await GraphTestRuntime.RunAsync(() => new AreaCreationForm { DataContext = state }, window =>
        {
            var form = (AreaCreationForm)window.Content!;
            var resRef = form.FindControl<TextBox>("ResRefInput")!;
            var displayName = form.FindControl<TextBox>("DisplayNameInput")!;
            var picker = form.FindControl<ComboBox>("TilesetPicker")!;
            var width = form.FindControl<NumericUpDown>("WidthInput")!;
            var height = form.FindControl<NumericUpDown>("HeightInput")!;
            var create = form.FindControl<Button>("CreateButton")!;
            var cancel = form.FindControl<Button>("CancelButton")!;

            Assert.AreEqual("area_one", resRef.Text);
            Assert.AreEqual("First Area", displayName.Text);
            Assert.AreEqual("New Area", ((TextBlock)((StackPanel)((Border)form.Content!).Child!).Children[0]).Text);
            Assert.AreEqual(2, picker.ItemCount);
            Assert.AreEqual(state.SelectedTileset, picker.SelectedItem);
            Assert.AreEqual(4, width.Value);
            Assert.AreEqual(3, height.Value);
            Assert.IsTrue(create.IsEnabled);
            Assert.IsTrue(cancel.IsEnabled);

            resRef.Text = "area_two";
            displayName.Text = "Second Area";
            picker.SelectedItem = state.Tilesets.Last();
            width.Value = 8;
            height.Value = 5;
            create.Command!.Execute(null);
            cancel.Command!.Execute(null);

            Assert.AreEqual("area_two", state.ResRef);
            Assert.AreEqual("Second Area", state.DisplayName);
            Assert.AreEqual(state.Tilesets.Last(), state.SelectedTileset);
            Assert.AreEqual(8, state.Width);
            Assert.AreEqual(5, state.Height);
            Assert.AreEqual(1, state.CreateCount);
            Assert.AreEqual(1, state.CancelCount);
        });
    }
}
