using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Resources;
using Nwn.Toolset.Avalonia.Tests.Support;

namespace Nwn.Toolset.Avalonia.Tests.Palettes.Workflow;

[TestClass]
public sealed class PaletteWorkflowPreviewTests
{
    [TestMethod]
    public async Task ACancelledPreviewRequestReachesTheHostsRenderer()
    {
        using var palette = new PaletteWorkflowFixture { Previews = new FakePalettePreviewSource() };
        palette.Content.AddCustom(ModuleResourceType.Utp, "crate");
        palette.Controller.Refresh();
        palette.SelectRow("Unsorted");
        palette.Previews.Tokens.Clear();
        using var cancellation = new CancellationTokenSource();

        var request = palette.Controller.LoadPreviewAsync(palette.Tile("crate").Snapshot, cancellation.Token).AsTask();
        var hostToken = palette.Previews.Tokens.Single();
        Assert.IsFalse(hostToken.IsCancellationRequested);

        cancellation.Cancel();

        Assert.IsTrue(hostToken.IsCancellationRequested, "The host's render is told to stop.");
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(() => request);
    }
}
