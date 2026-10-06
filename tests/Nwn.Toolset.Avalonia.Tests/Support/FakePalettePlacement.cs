using Nwn.Toolset.Avalonia.Palettes.Workflow;

namespace Nwn.Toolset.Avalonia.Tests.Support;

internal sealed class FakePalettePlacement : IPalettePlacementTargetProvider
{
    public IPalettePlacementTarget? ActiveTarget { get; set; }
}
