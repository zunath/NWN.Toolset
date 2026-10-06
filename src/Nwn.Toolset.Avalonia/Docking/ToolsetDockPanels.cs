using Dock.Model.Core;

namespace Nwn.Toolset.Avalonia.Docking;

/// <summary>Host-owned tool instances placed in the shared builder rails.</summary>
public sealed record ToolsetDockPanels(
    IReadOnlyList<IDockable> ExplorerTools,
    IReadOnlyList<IDockable> PaletteTools,
    IReadOnlyList<IDockable> OutputTools,
    IReadOnlyList<IDockable>? AdditionalContextTools = null);
