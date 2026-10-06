using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Categories;
using Nwn.Authoring.Resources;
using Nwn.Toolset.Avalonia.Palettes;
using Nwn.Toolset.Avalonia.Tests.Support;

namespace Nwn.Toolset.Avalonia.Tests.Palettes.Workflow;

/// <summary>
/// Create and Edit Copy wait for a host's asynchronous write instead of making it block, carry the caller's
/// cancellation to it, and accept a copy the host hands to an editor rather than writes.
/// </summary>
[TestClass]
public sealed class PaletteWorkflowAsyncWriteTests
{
    [TestMethod]
    public async Task EditCopyWaitsForTheHostsWriteBeforeFilingIt()
    {
        using var palette = StandardBoxPalette();
        var write = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        palette.Blueprints.WriteGate = write.Task;
        using var cancellation = new CancellationTokenSource();

        var copying = palette.Controller.EditCopyAsync(palette.Tile("std_box").Snapshot, cancellation.Token);

        Assert.IsFalse(copying.IsCompleted, "The palette waits for the host's write.");
        Assert.AreEqual(cancellation.Token, palette.Blueprints.WriteTokens.Single());
        Assert.IsNull(palette.Categories.Live.Section(ModuleResourceType.Utp).Find("Containers", "Boxes"));

        write.SetResult();
        await copying;

        CollectionAssert.AreEqual(
            new[] { "std_box001" },
            palette.Categories.Live.Section(ModuleResourceType.Utp).Find("Containers", "Boxes")!.Members.ToArray());
        Assert.AreEqual("std_box001", palette.State.SelectedTile?.ResRef);
        CollectionAssert.AreEqual(new[] { "std_box001" }, palette.Blueprints.OpenedEditors);
    }

    [TestMethod]
    public async Task ACancelledCopyFilesAndOpensNothing()
    {
        using var palette = StandardBoxPalette();
        palette.Blueprints.WriteGate = new TaskCompletionSource().Task;
        using var cancellation = new CancellationTokenSource();

        var copying = palette.Controller.EditCopyAsync(palette.Tile("std_box").Snapshot, cancellation.Token);
        cancellation.Cancel();

        await Assert.ThrowsExactlyAsync<TaskCanceledException>(() => copying);
        Assert.AreEqual(0, palette.Blueprints.Copied.Count);
        Assert.AreEqual(0, palette.Blueprints.OpenedEditors.Count);
        Assert.AreEqual(0, palette.Categories.Saves);
    }

    [TestMethod]
    public async Task NewBlueprintWaitsForTheHostsWriteAndFilesByPathIntoAReplacedTree()
    {
        using var palette = new PaletteWorkflowFixture();
        palette.Categories.Live.Section(ModuleResourceType.Utp).AddFolder("Boxes");
        palette.Categories.Commit();
        palette.Controller.Refresh();
        var boxes = palette.SelectRow("Boxes");
        palette.Prompts.Answers.Enqueue("Big Crate");
        var write = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        palette.Blueprints.WriteGate = write.Task;

        palette.Controller.NewBlueprint(boxes.Id);
        var creating = palette.Controller.NewBlueprintCommand.ExecutionTask!;
        Assert.IsFalse(creating.IsCompleted);

        // The host reloads its sidecar while the write is in flight: the folder object the palette saw is gone.
        palette.Categories.ReplaceLive();
        write.SetResult();
        await creating;

        CollectionAssert.AreEqual(
            new[] { "big_crate" },
            palette.Categories.Live.Section(ModuleResourceType.Utp).Find("Boxes")!.Members.ToArray());
        Assert.AreEqual("Created Big Crate.", palette.Controller.StatusMessage);
        CollectionAssert.AreEqual(new[] { "big_crate" }, palette.Blueprints.OpenedEditors);
    }

    [TestMethod]
    public void EditCopyHandedToAnEditorIsReportedWithoutFilingRevealingOrASecondEditor()
    {
        using var palette = StandardBoxPalette();
        palette.Blueprints.CopiesInEditor = true;

        palette.Controller.EditCopy(palette.Tile("std_box").Snapshot);

        Assert.AreEqual((PaletteSource.Standard, "std_box"), palette.Blueprints.Copied.Single());
        Assert.AreEqual("Opened a copy of Box in its editor.", palette.Controller.StatusMessage);
        CollectionAssert.Contains(palette.Log.Lines, "Edit Copy handed utp blueprint 'std_box' to its editor's copy mode.");
        Assert.AreEqual(0, palette.Blueprints.OpenedEditors.Count, "The host already opened the editor.");
        Assert.AreEqual(0, palette.Categories.Saves);
        Assert.AreEqual(PaletteSource.Standard, palette.Controller.Source, "There is no copy to reveal yet.");
    }

    private static PaletteWorkflowFixture StandardBoxPalette()
    {
        var palette = new PaletteWorkflowFixture();
        var standard = new CategorySection();
        standard.AddFolder("Containers").AddChild("Boxes").AddMember("std_box");
        palette.Content.StandardPalettes[ModuleResourceType.Utp] = new StandardPalette(
            standard,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "std_box" },
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["std_box"] = "Box" });
        palette.Controller.SelectSource(PaletteSource.Standard);
        palette.SelectRow("Boxes");
        return palette;
    }
}
