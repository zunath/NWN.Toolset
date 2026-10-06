using CommunityToolkit.Mvvm.ComponentModel;

namespace Nwn.Toolset.Avalonia.Areas
{
    /// <summary>
    /// The view-only display switches shared by every open area viewport in one toolset window.
    /// </summary>
    /// <remarks>
    /// Global rather than per-area: they say how the builder wants to look at the module, not
    /// anything about a particular area, and two open areas disagreeing about fog would only be
    /// confusing. Persistence is injected, so each host keeps its own storage.
    /// </remarks>
    public sealed partial class AreaViewportDisplayOptions : ObservableObject
    {
        private readonly IAreaViewportDisplayPersistence? _persistence;
        private readonly bool _loading;

        public AreaViewportDisplayOptions(IAreaViewportDisplayPersistence? persistence = null)
        {
            _persistence = persistence;

            var settings = persistence?.Load() ?? AreaViewportDisplaySettings.Default;
            _loading = true;
            try
            {
                _showAreaLighting = settings.ShowAreaLighting;
                _showFog = settings.ShowFog;
                _showCeilings = settings.ShowCeilings;
                _showMaterialMaps = settings.ShowMaterialMaps;
            }
            finally
            {
                _loading = false;
            }
        }

        /// <summary>
        /// Light the scene with the area's own sun/moon colours rather than a neutral editor light.
        /// Off by default: a night area's authored light is close to black and buries the textures.
        /// </summary>
        [ObservableProperty]
        private bool _showAreaLighting;

        /// <summary>Apply the area's distance fog. Off by default: fog hides the far geometry being placed.</summary>
        [ObservableProperty]
        private bool _showFog;

        /// <summary>
        /// Draw an interior tileset's ceilings instead of looking into its rooms from above. Off by
        /// default, matching Aurora.
        /// </summary>
        [ObservableProperty]
        private bool _showCeilings;

        /// <summary>
        /// Render normal/specular/roughness material maps on textured meshes. On by default: it is
        /// what the game itself renders.
        /// </summary>
        [ObservableProperty]
        private bool _showMaterialMaps = true;

        /// <summary>The four switches as one value.</summary>
        public AreaViewportDisplaySettings Settings => new(
            ShowAreaLighting, ShowFog, ShowCeilings, ShowMaterialMaps);

        /// <summary>Pushes the four switches onto <paramref name="viewport"/>.</summary>
        public void ApplyTo(AreaViewportControl viewport)
        {
            ArgumentNullException.ThrowIfNull(viewport);
            viewport.ShowAreaLighting = ShowAreaLighting;
            viewport.ShowFog = ShowFog;
            viewport.ShowCeilings = ShowCeilings;
            viewport.ShowMaterialMaps = ShowMaterialMaps;
        }

        partial void OnShowAreaLightingChanged(bool value) => Persist();

        partial void OnShowFogChanged(bool value) => Persist();

        partial void OnShowCeilingsChanged(bool value) => Persist();

        partial void OnShowMaterialMapsChanged(bool value) => Persist();

        private void Persist()
        {
            if (_loading || _persistence == null)
                return;

            _persistence.Save(Settings);
        }
    }
}
