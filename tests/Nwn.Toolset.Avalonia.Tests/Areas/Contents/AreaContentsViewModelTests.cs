using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Nwn.Toolset.Avalonia.Areas.Contents.Views;
using Nwn.Toolset.Avalonia.Tests.Support;
using Nwn.Authoring.Resources;
using Nwn.Toolset.Avalonia.Areas.Contents;
using Nwn.Toolset.Avalonia.Localization;

namespace Nwn.Toolset.Avalonia.Tests.Areas.Contents;

[TestClass]
public sealed class AreaContentsViewModelTests
{
    [TestMethod]
    public void GroupingFilteringAndHostActionsKeepTypedPlacementIdentity()
    {
        var actions = new RecordingActions();
        var model = CreateModel();
        model.SetContents(Snapshot(3), actions);

        var kind = model.Rows.Single(row => row.Kind == AreaContentsNodeKind.Kind);
        Assert.AreEqual(1, kind.Children.Count);
        var group = kind.Children.Single();
        Assert.AreEqual(AreaContentsNodeKind.Group, group.Kind);
        model.ToggleCommand.Execute(group);
        Assert.AreEqual(5, model.Rows.Count);

        var instance = group.Children[0];
        model.SelectedRow = instance;
        var identity = new AreaContentsIdentity(ModuleResourceType.Utc, 0);
        Assert.AreEqual(identity, actions.Selected);
        model.OpenCommand.Execute(instance);
        Assert.AreEqual(identity, actions.Framed);
        model.OpenPropertiesCommand.Execute(group);
        Assert.AreEqual(identity, actions.Properties);

        model.Filter = "tag-2";
        var filteredKind = model.Rows.Single(row => row.Kind == AreaContentsNodeKind.Kind);
        if (!filteredKind.IsExpanded)
            model.ToggleCommand.Execute(filteredKind);
        Assert.IsTrue(model.Rows.Any(row =>
            row.Kind == AreaContentsNodeKind.Instance && row.Identities.Single().InstanceIndex == 2));
        StringAssert.Contains(model.StatusMessage, "1 of 3");
    }

    [TestMethod]
    public void ForcedRevealRealizesAnEntryPastTheTwoHundredRowTail()
    {
        var model = CreateModel();
        model.SetContents(Snapshot(205), new RecordingActions());
        model.Filter = "missing";
        var target = new AreaContentsIdentity(ModuleResourceType.Utc, 204);

        model.Reveal(target);

        Assert.AreEqual(string.Empty, model.Filter);
        Assert.AreEqual(target, model.SelectedRow!.Identities.Single());
        Assert.IsTrue(model.Rows.Contains(model.SelectedRow));
        Assert.IsTrue(model.TryTakePendingRowReveal(out var pending));
        Assert.AreSame(model.SelectedRow, pending);
    }

    [TestMethod]
    public async Task DelayedDeleteDoesNotClearASelectionMadeWhileHostMutationWaits()
    {
        var actions = new RecordingActions
        {
            DeleteCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)
        };
        var model = CreateModel();
        model.SetContents(Snapshot(2), actions);
        var kind = model.Rows.Single(row => row.Kind == AreaContentsNodeKind.Kind);
        if (!kind.IsExpanded)
            model.ToggleCommand.Execute(kind);
        var group = model.Rows.Single(row => row.Kind == AreaContentsNodeKind.Group);
        model.ToggleCommand.Execute(group);
        model.SelectedRow = model.Rows.Single(row =>
            row.Kind == AreaContentsNodeKind.Instance && row.Identities.Single().InstanceIndex == 0);

        var requested = model.SelectedRow;
        var delete = model.DeleteSelectedCommand.ExecuteAsync(null);
        model.SelectedRow = model.Rows.Single(row =>
            row.Kind == AreaContentsNodeKind.Instance && row.Identities.Single().InstanceIndex == 1);
        actions.DeleteCompletion.SetResult();
        await delete;

        Assert.AreNotSame(requested, model.SelectedRow);
        Assert.AreEqual(1, model.SelectedRow!.Identities.Single().InstanceIndex);
    }

    [TestMethod]
    public async Task SharedViewMountsAndExpandsTheTypedTreeRows()
    {
        var model = CreateModel();
        model.SetContents(Snapshot(2), new RecordingActions());
        await GraphTestRuntime.RunAsync(() => new AreaContentsView { Contents = model }, window =>
        {
            var list = window.GetVisualDescendants().OfType<ListBox>().Single();
            Assert.AreEqual(2, list.Items.Count);
            Assert.AreEqual(AreaContentsNodeKind.Kind, ((AreaContentsNodeViewModel)list.Items[0]!).Kind);
            Assert.AreEqual(AreaContentsNodeKind.Group, ((AreaContentsNodeViewModel)list.Items[1]!).Kind);
            var group = model.Rows.Single(row => row.Kind == AreaContentsNodeKind.Group);
            var twisty = list.GetVisualDescendants().OfType<Button>()
                .Single(button => ReferenceEquals(button.DataContext, group));
            Assert.IsNotNull(twisty.Command);
            Assert.IsInstanceOfType(twisty.CommandParameter, typeof(AreaContentsNodeViewModel));
            Assert.AreSame(group, twisty.CommandParameter);
            twisty.Command!.Execute(twisty.CommandParameter);
            Dispatcher.UIThread.RunJobs();
            Assert.AreEqual(4, list.Items.Count);
            Assert.IsTrue(model.Rows.Contains(group.Children[0]));
        });
    }
    private static AreaContentsViewModel CreateModel() =>
        new(new AreaContentsTexts(Enum.GetValues<AreaContentsStringId>().ToDictionary(id => id, TextFor)));

    private static string TextFor(AreaContentsStringId id) => id switch
    {
        AreaContentsStringId.StatusObjects => "{0} — {1} objects",
        AreaContentsStringId.StatusMatches => "{0} — {1} of {2} match “{3}”",
        AreaContentsStringId.Overflow => "… {0} more",
        AreaContentsStringId.KindMatches => "{0} of {1}",
        AreaContentsStringId.KindGroups => "{0} · {1}",
        AreaContentsStringId.GroupCount => "×{0}",
        _ => id.ToString()
    };

    private static AreaContentsSnapshot Snapshot(int count)
    {
        var entries = Enumerable.Range(0, count)
            .Select(index => new AreaContentsEntry(
                ModuleResourceType.Utc, index, "Same name", "test_creature", $"tag-{index}",
                new Vector3(index, 4, 0)))
            .ToArray();
        var sections = new[] { new AreaContentsSection(ModuleResourceType.Utc, "Creatures", entries) };
        return new AreaContentsSnapshot("contents_test", sections);
    }

    private sealed class RecordingActions : IAreaContentsActions
    {
        public AreaContentsIdentity? Selected { get; private set; }
        public AreaContentsIdentity? Framed { get; private set; }
        public AreaContentsIdentity? Properties { get; private set; }
        public TaskCompletionSource? DeleteCompletion { get; init; }
        public void Select(AreaContentsIdentity identity) => Selected = identity;
        public void Frame(AreaContentsIdentity identity) => Framed = identity;
        public void OpenProperties(AreaContentsIdentity identity) => Properties = identity;
        public async Task<bool> DeleteAsync(
            IReadOnlyList<AreaContentsIdentity> identities,
            string displayName,
            CancellationToken cancellationToken = default)
        {
            if (DeleteCompletion is not null)
                await DeleteCompletion.Task;
            return true;
        }
    }
}



