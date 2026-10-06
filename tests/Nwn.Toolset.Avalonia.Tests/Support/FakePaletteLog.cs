using Nwn.Toolset.Avalonia.Palettes.Workflow;

namespace Nwn.Toolset.Avalonia.Tests.Support;

internal sealed class FakePaletteLog : IPaletteLog
{
    public List<string> Lines { get; } = new();

    public void Write(string message) => Lines.Add(message);
}
