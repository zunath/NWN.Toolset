using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Nwn.Toolset.Avalonia.RenderSmoke.Viewport;

namespace Nwn.Toolset.Avalonia.RenderSmoke.Application;

internal sealed class RenderSmokeApplication : global::Avalonia.Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            Console.Error.WriteLine("Desktop lifetime was not initialized.");
            base.OnFrameworkInitializationCompleted();
            return;
        }

        var surface = new ReadbackViewportSurface();
        surface.RenderCheckCompleted += (passed, message) => Dispatcher.UIThread.Post(() =>
        {
            Console.WriteLine(message);
            desktop.Shutdown(passed ? 0 : 1);
        });
        var window = new Window
        {
            Title = "Nwn.Toolset.Avalonia GL render smoke",
            Width = 512,
            Height = 512,
            Content = surface
        };
        var timeout = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        timeout.Tick += (_, _) =>
        {
            timeout.Stop();
            Console.Error.WriteLine("Timed out waiting for a frame from the OpenGL control.");
            desktop.Shutdown(2);
        };
        timeout.Start();
        window.Show();
        base.OnFrameworkInitializationCompleted();
    }
}
