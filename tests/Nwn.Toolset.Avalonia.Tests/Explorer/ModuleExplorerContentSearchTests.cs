using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Resources;
using Nwn.Toolset.Avalonia.Explorer;
using Nwn.Toolset.Avalonia.Explorer.Workflow;
using Nwn.Toolset.Avalonia.Tests.Support;

namespace Nwn.Toolset.Avalonia.Tests.Explorer;

[TestClass]
public sealed class ModuleExplorerContentSearchTests
{
    [TestMethod]
    public Task ContentMatchesJoinNameMatchesAfterTheDebouncedScan() => GraphTestRuntime.DispatchAsync(async () =>
    {
        using var fixture = new ModuleExplorerFixture();
        fixture.Content.Add(ModuleResourceType.Dlg, "veldite_terminal");
        fixture.Content.Add(ModuleResourceType.Dlg, "mining");
        fixture.Search.Content["mining"] = "The Veldite seam runs deep.";
        fixture.SeededSection(ModuleResourceType.Dlg);
        var controller = fixture.Open(ModuleResourceType.Dlg);
        controller.ToggleCommand.Execute(fixture.Row("Unsorted"));

        var stopwatch = Stopwatch.StartNew();
        controller.Filter = "veldite";

        Assert.IsTrue(controller.IsSearchingContent);
        Assert.AreEqual("Searching dialogue...", controller.ContentSearchLabel);
        Assert.IsTrue(Contains(controller, "veldite_terminal"), "name matches never wait for the scan");
        Assert.IsFalse(Contains(controller, "mining"));

        await WaitUntilAsync(() => !controller.IsSearchingContent);
        stopwatch.Stop();

        Assert.IsTrue(Contains(controller, "mining"));
        Assert.IsTrue(stopwatch.Elapsed >= TimeSpan.FromMilliseconds(200), "the debounce is awaited before the scan");
    });

    [TestMethod]
    public Task TheScanIsPreparedOnTheCallingThreadAndRunsOnAWorker() => GraphTestRuntime.DispatchAsync(async () =>
    {
        using var fixture = new ModuleExplorerFixture();
        fixture.Content.Add(ModuleResourceType.Dlg, "mining");
        fixture.Search.Content["mining"] = "Veldite";
        var controller = fixture.Open(ModuleResourceType.Dlg);
        var uiThread = Environment.CurrentManagedThreadId;

        controller.Filter = "veldite";
        await WaitUntilAsync(() => !controller.IsSearchingContent);

        Assert.AreEqual(uiThread, fixture.Search.PreparedOnThreads.Single(), "open-editor state is snapshotted on the UI thread");
        Assert.AreNotEqual(uiThread, fixture.Search.RanOnThreads.Single(), "the corpus is read off the UI thread");
    });

    [TestMethod]
    public Task TypingAWordScansOnlyOnceTypingPauses() => GraphTestRuntime.DispatchAsync(async () =>
    {
        using var fixture = new ModuleExplorerFixture();
        fixture.Content.Add(ModuleResourceType.Dlg, "mining");
        var controller = fixture.Open(ModuleResourceType.Dlg);

        foreach (var prefix in new[] { "v", "ve", "vel", "veld" })
            controller.Filter = prefix;
        await WaitUntilAsync(() => !controller.IsSearchingContent);

        Assert.AreEqual(1, fixture.Search.RanOnThreads.Count, "abandoned prefixes are cancelled before they read anything");
    });

    [TestMethod]
    public Task AChangedResourceRerunsTheSameQuery() => GraphTestRuntime.DispatchAsync(async () =>
    {
        using var fixture = new ModuleExplorerFixture();
        fixture.Content.Add(ModuleResourceType.Dlg, "greeting");
        fixture.SeededSection(ModuleResourceType.Dlg);
        var controller = fixture.Open(ModuleResourceType.Dlg);
        controller.ToggleCommand.Execute(fixture.Row("Unsorted"));
        controller.Filter = "veldite";
        await WaitUntilAsync(() => !controller.IsSearchingContent);
        Assert.IsFalse(Contains(controller, "greeting"));

        fixture.Search.Content["greeting"] = "The Veldite seam.";
        fixture.Content.RaiseResourceChanged(ModuleResourceType.Dlg, "greeting");

        Assert.IsTrue(controller.IsSearchingContent);
        await WaitUntilAsync(() => !controller.IsSearchingContent);
        controller.Refresh();
        Assert.IsTrue(Contains(controller, "greeting"));
    });

    [TestMethod]
    public Task AFailedScanIsReportedInTheStatusLine() => GraphTestRuntime.DispatchAsync(async () =>
    {
        using var fixture = new ModuleExplorerFixture();
        fixture.Content.Add(ModuleResourceType.Dlg, "greeting");
        fixture.Search.Failure = "unreadable";
        var controller = fixture.Open(ModuleResourceType.Dlg);

        controller.Filter = "anything";
        await WaitUntilAsync(() => !controller.IsSearchingContent);

        Assert.AreEqual("Dialogue search failed: unreadable", controller.StatusMessage);
    });

    [TestMethod]
    public void TabsWithoutContentSearchNeverScan()
    {
        using var fixture = new ModuleExplorerFixture();
        fixture.Content.Add(ModuleResourceType.Nss, "veldite_script");
        var controller = fixture.Open(ModuleResourceType.Nss);

        controller.Filter = "veldite";

        Assert.IsFalse(controller.IsSearchingContent);
        Assert.AreEqual(0, fixture.Search.Prepares);
        Assert.AreEqual(string.Empty, controller.ContentSearchLabel);
    }

    private static bool Contains(ModuleExplorerController controller, string resRef) =>
        controller.Rows.Any(row => Contains(row, resRef));

    private static bool Contains(ExplorerNodeViewModel row, string resRef) =>
        row.ResRef == resRef || row.Children.Any(child => Contains(child, resRef));

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!condition())
        {
            if (DateTime.UtcNow >= deadline)
                Assert.Fail("Timed out waiting for the content search to settle.");

            await Task.Delay(25);
        }
    }
}
