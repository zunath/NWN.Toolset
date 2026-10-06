using Avalonia;
using Avalonia.Headless;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Themes.Fluent;

namespace Nwn.Toolset.Avalonia.Tests.Support;

public sealed class GraphTestApplication : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        Nwn.Toolset.Avalonia.Docking.RailToolTabs.Register();
        Resources.MergedDictionaries.Add(new ResourceInclude(new Uri("avares://Nwn.Toolset.Avalonia/"))
        {
            Source = new Uri("avares://Dock.Avalonia.Themes.Fluent/Presets/Ide/Default.axaml"),
        });
        Resources.MergedDictionaries.Add(new ResourceInclude(new Uri("avares://Nwn.Toolset.Avalonia/"))
        {
            Source = new Uri("avares://Nwn.Toolset.Avalonia/Styles/ToolsetIcons.axaml"),
        });
        Styles.Add(new StyleInclude(new Uri("avares://Nwn.Toolset.Avalonia.Tests/"))
        {
            Source = new Uri("avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml"),
        });
        Styles.Add(new StyleInclude(new Uri("avares://Nwn.Toolset.Avalonia/"))
        {
            Source = new Uri("avares://Nwn.Toolset.Avalonia/Styles/ToolsetStyles.axaml"),
        });
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<GraphTestApplication>()
        .UseSkia().WithInterFont().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
