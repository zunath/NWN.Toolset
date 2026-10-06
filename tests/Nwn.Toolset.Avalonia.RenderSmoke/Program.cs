using Avalonia;
using Nwn.Toolset.Avalonia.RenderSmoke.Application;

namespace Nwn.Toolset.Avalonia.RenderSmoke;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args) => AppBuilder.Configure<RenderSmokeApplication>()
        .UsePlatformDetect()
        .StartWithClassicDesktopLifetime(args);
}
