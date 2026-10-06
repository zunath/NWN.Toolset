namespace Nwn.Toolset.Avalonia.Palettes.Workflow;

/// <summary>The host's output log for palette diagnostics.</summary>
public interface IPaletteLog
{
    void Write(string message);
}
