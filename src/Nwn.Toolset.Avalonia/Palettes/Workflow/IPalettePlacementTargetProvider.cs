namespace Nwn.Toolset.Avalonia.Palettes.Workflow;

/// <summary>Finds the area currently in front, asked on demand because it changes with the active tab.</summary>
public interface IPalettePlacementTargetProvider
{
    /// <summary>The area in front, or null when none is open.</summary>
    IPalettePlacementTarget? ActiveTarget { get; }
}
