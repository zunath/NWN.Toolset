namespace Nwn.Toolset.Avalonia.Areas
{
    /// <summary>
    /// The four view-only display switches an area viewport offers, as one value a host can load and
    /// save. Defaults match Aurora: lighting, fog and ceilings are off so the geometry being placed
    /// stays readable, and material maps are on because that is what the game renders.
    /// </summary>
    public readonly record struct AreaViewportDisplaySettings(
        bool ShowAreaLighting,
        bool ShowFog,
        bool ShowCeilings,
        bool ShowMaterialMaps)
    {
        /// <summary>The settings used when a host has nothing stored.</summary>
        public static AreaViewportDisplaySettings Default { get; } = new(
            ShowAreaLighting: false,
            ShowFog: false,
            ShowCeilings: false,
            ShowMaterialMaps: true);
    }
}
