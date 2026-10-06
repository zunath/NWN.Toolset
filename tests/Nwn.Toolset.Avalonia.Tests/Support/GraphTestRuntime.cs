using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;

namespace Nwn.Toolset.Avalonia.Tests.Support;

[TestClass]
public sealed class GraphTestRuntime
{
    private static HeadlessUnitTestSession _session = null!;

    [AssemblyInitialize]
    public static void Initialize(TestContext _) =>
        _session = HeadlessUnitTestSession.StartNew(typeof(GraphTestApplication), AvaloniaTestIsolationLevel.PerAssembly);

    [AssemblyCleanup]
    public static void Cleanup() => _session.Dispose();

    public static Task RunAwaitedAsync(Func<Control> create, Func<Window, Task> assertions) => _session.Dispatch(async () =>
    {
        var window = new Window { Width = 1000, Height = 650, Content = create() };
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            await assertions(window);
        }
        finally
        {
            window.Close();
            Dispatcher.UIThread.RunJobs();
        }
        return true;
    }, CancellationToken.None);

    /// <summary>Runs work on the headless UI thread without a window, for code that posts to the dispatcher.</summary>
    public static Task DispatchAsync(Func<Task> work) => _session.Dispatch(async () =>
    {
        await work();
        return true;
    }, CancellationToken.None);

    public static Task RunAsync(Func<Control> create, Action<Window> assertions) => _session.Dispatch(() =>
    {
        var window = new Window { Width = 1000, Height = 650, Content = create() };
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            assertions(window);
        }
        finally
        {
            window.Close();
            Dispatcher.UIThread.RunJobs();
        }
    }, CancellationToken.None);
}
