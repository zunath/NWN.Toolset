// SPDX-License-Identifier: MIT

using System.ComponentModel;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Nwn.Toolset.Avalonia.Areas;
using Nwn.Toolset.Avalonia.Tests.Support;

namespace Nwn.Toolset.Avalonia.Tests.Areas;

[TestClass]
public sealed class AreaSelectionContextMenuTests
{
    [TestMethod]
    public async Task MenuTracksCurrentSelectionAndKeepsUnavailableActionsDisabledAsync()
    {
        var state = new SelectionState();
        await GraphTestRuntime.RunAsync(() => new Button { ContextMenu = new AreaSelectionContextMenu(state) }, window =>
        {
            var host = (Button)window.Content!;
            var menu = (AreaSelectionContextMenu)host.ContextMenu!;
            menu.Open(host);
            Dispatcher.UIThread.RunJobs();

            var items = menu.Items.OfType<MenuItem>().ToArray();
            Assert.AreEqual(5, items.Length);
            var name = items[0];
            var kind = items[1];
            Assert.AreEqual("First object", name.Header);
            Assert.AreEqual("Placeable", kind.Header);
            Assert.AreEqual("crate_a", ToolTip.GetTip(kind));
            Assert.IsTrue(items[2].IsEnabled);
            Assert.IsFalse(items[3].IsEnabled);
            Assert.IsFalse(items[4].IsEnabled);

            state.SetSelection("Second object", "D", "Door", "door_a");
            Dispatcher.UIThread.RunJobs();
            Assert.AreEqual("Second object", name.Header);
            Assert.AreEqual("Door", kind.Header);
            Assert.AreEqual("door_a", ToolTip.GetTip(kind));
            Assert.IsFalse(items[2].IsEnabled, "The action can become unavailable after the menu is created.");
            Assert.IsTrue(items[3].IsEnabled);
            Assert.IsTrue(items[4].IsEnabled);
            Assert.AreSame(state.EditBlueprintCommand, items[3].Command);
        });
    }

    private sealed class SelectionState : IAreaSelectionContextMenuState
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public string SelectionName { get; private set; } = "First object";
        public string SelectionGlyph { get; private set; } = "P";
        public string SelectionKindLabel { get; private set; } = "Placeable";
        public string SelectionResRef { get; private set; } = "crate_a";
        public bool CanOpenProperties { get; private set; } = true;
        public bool CanEditBlueprint { get; private set; }
        public bool CanEditCopy { get; private set; }
        public ICommand OpenPropertiesCommand { get; } = new TestCommand(canExecute: true);
        public ICommand EditBlueprintCommand { get; } = new TestCommand(canExecute: false);
        public ICommand EditCopyCommand { get; } = new TestCommand(canExecute: false);

        public void SetSelection(string name, string glyph, string kind, string resRef)
        {
            SelectionName = name;
            SelectionGlyph = glyph;
            SelectionKindLabel = kind;
            SelectionResRef = resRef;
            CanOpenProperties = false;
            CanEditBlueprint = true;
            CanEditCopy = true;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectionName)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectionGlyph)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectionKindLabel)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectionResRef)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanOpenProperties)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanEditBlueprint)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanEditCopy)));
            ((TestCommand)OpenPropertiesCommand).SetCanExecute(false);
            ((TestCommand)EditBlueprintCommand).SetCanExecute(true);
            ((TestCommand)EditCopyCommand).SetCanExecute(true);
        }
    }

    private sealed class TestCommand(bool canExecute) : ICommand
    {
        private bool _canExecute = canExecute;
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => _canExecute;
        public void Execute(object? parameter) { }
        public void SetCanExecute(bool canExecute)
        {
            _canExecute = canExecute;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
