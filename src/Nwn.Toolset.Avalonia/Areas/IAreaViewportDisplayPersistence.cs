namespace Nwn.Toolset.Avalonia.Areas
{
    /// <summary>
    /// How a host stores <see cref="AreaViewportDisplaySettings"/>. The shared options model owns the
    /// switches; where they live (a settings file, a workspace sidecar) is the host's decision.
    /// </summary>
    public interface IAreaViewportDisplayPersistence
    {
        /// <summary>Reads the stored settings, or <see cref="AreaViewportDisplaySettings.Default"/> values for anything missing.</summary>
        AreaViewportDisplaySettings Load();

        /// <summary>
        /// Stores the settings after a switch changed. Called on the thread that changed the switch;
        /// a host that can fail reports the failure through its own surface rather than throwing.
        /// </summary>
        void Save(AreaViewportDisplaySettings settings);
    }
}
