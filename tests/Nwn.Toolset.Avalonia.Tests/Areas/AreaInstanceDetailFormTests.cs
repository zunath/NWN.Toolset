using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Nwn.Toolset.Avalonia.Areas;
using Nwn.Toolset.Avalonia.Tests.Support;

namespace Nwn.Toolset.Avalonia.Tests.Areas;

[TestClass]
public sealed class AreaInstanceDetailFormTests
{
    [TestMethod]
    public async Task ExistingDetailFieldsBindToEitherHostAndFollowSelectionReplacement()
    {
        var state = new HostState { DetailTag = "first", DetailX = 2, DetailTriggerWidth = 3, HasTriggerGeometry = true };
        await GraphTestRuntime.RunAsync(() => new AreaInstanceDetailForm { DataContext = state }, window =>
        {
            var form = (AreaInstanceDetailForm)window.Content!;
            window.Width = 330;
            window.UpdateLayout();
            foreach (var name in new[] { "XInput", "YInput", "ZInput", "FacingXInput", "FacingYInput", "WidthInput", "HeightInput" })
            {
                var input = form.FindControl<NumericUpDown>(name)!;
                Assert.IsTrue(input.Bounds.Width >= 90);
                var position = input.TranslatePoint(default, window);
                Assert.IsNotNull(position);
                Assert.IsTrue(position.Value.X >= 0 && position.Value.X + input.Bounds.Width <= window.ClientSize.Width);
            }
            Assert.AreEqual("first", form.FindControl<TextBox>("TagInput")!.Text);
            Assert.AreEqual(2m, form.FindControl<NumericUpDown>("XInput")!.Value);
            Assert.AreEqual(3m, form.FindControl<NumericUpDown>("WidthInput")!.Value);
            form.FindControl<TextBox>("TagInput")!.Text = "edited";
            form.FindControl<NumericUpDown>("XInput")!.Value = 7.5m;
            form.FindControl<NumericUpDown>("FacingYInput")!.Value = 0.5m;
            form.FindControl<NumericUpDown>("WidthInput")!.Value = 4m;
            Assert.AreEqual("edited", state.DetailTag);
            Assert.AreEqual(7.5, state.DetailX);
            Assert.AreEqual(0.5, state.DetailYOrientation);
            Assert.AreEqual(4, state.DetailTriggerWidth);
            var replacement = new HostState { DetailTag = "second" };
            form.DataContext = replacement;
            Assert.AreEqual("second", form.FindControl<TextBox>("TagInput")!.Text);
            Assert.IsFalse(form.FindControl<NumericUpDown>("WidthInput")!.IsEffectivelyVisible);
            form.FindControl<TextBox>("TagInput")!.Text = "second edit";
            Assert.AreEqual("second edit", replacement.DetailTag);
            Assert.AreEqual("edited", state.DetailTag);
        });
    }

    private sealed class HostState : IAreaInstanceDetailFormState
    {
        public event PropertyChangedEventHandler? PropertyChanged { add { } remove { } }
        public string DetailTag { get; set; } = string.Empty;
        public double DetailX { get; set; }
        public double DetailY { get; set; }
        public double DetailZ { get; set; }
        public double DetailXOrientation { get; set; }
        public double DetailYOrientation { get; set; }
        public double DetailTriggerWidth { get; set; }
        public double DetailTriggerHeight { get; set; }
        public bool UsesGenericDetailEditor => true;
        public bool HasTriggerGeometry { get; set; }
        public AreaInstanceDetailLabels InstanceLabels { get; } = new("Tag", "X", "Y", "Z", "Facing X", "Facing Y", "Width", "Height");
    }
}
