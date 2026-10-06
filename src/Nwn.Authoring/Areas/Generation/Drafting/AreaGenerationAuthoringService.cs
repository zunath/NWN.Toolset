#nullable enable
using Nwn.Authoring.Areas.Generation.Composition;
using Nwn.Authoring.Areas.Generation.Hosting;
using Nwn.Authoring.Areas.Generation.Population;
using Nwn.Authoring.Resources;

namespace Nwn.Authoring.Areas.Generation.Drafting
{
    /// <summary>
    /// Resolves the host's catalog and tilesets, then produces a deterministic draft suitable for
    /// preview or direct creation. Every game-specific rule arrives through the catalog, tileset
    /// source and blueprint source; this class holds none.
    /// </summary>
    public sealed class AreaGenerationAuthoringService
    {
        private readonly IAreaGenerationTilesetSource _tilesets;
        private readonly IAreaGenerationLog _log;

        public IAreaGenerationCatalog Catalog { get; }

        public AreaGenerationAuthoringService(
            IAreaGenerationTilesetSource tilesets,
            IAreaGenerationCatalog catalog,
            IAreaGenerationLog? log = null)
        {
            _tilesets = tilesets ?? throw new ArgumentNullException(nameof(tilesets));
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _log = log ?? NullAreaGenerationLog.Instance;
        }

        /// <summary>
        /// Produces a deterministic area draft from the requested composition, layout settings and decoration policy.
        /// A catalog without themes composes the requested tileset and layout profiles with no content package, which
        /// yields geometry only.
        /// </summary>
        public AreaGenerationDraft Generate(AreaGenerationSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            DungeonDetail? theme = null;
            if (Catalog.Themes.Count > 0)
            {
                theme = Catalog.Themes.FirstOrDefault(candidate =>
                    candidate.ThemeKey.Equals(settings.ThemeKey, StringComparison.OrdinalIgnoreCase));
                if (theme == null)
                    throw new ArgumentException($"Unknown area theme '{settings.ThemeKey}'.", nameof(settings));
                ValidateTheme(theme, settings);
            }

            var tilesetKey = string.IsNullOrWhiteSpace(settings.TilesetProfileKey)
                ? theme?.TilesetProfileKey ?? string.Empty
                : settings.TilesetProfileKey;
            if (!Catalog.TilesetProfiles.TryGetValue(tilesetKey, out var tilesetProfile))
                throw new ArgumentException($"Unknown tileset profile '{tilesetKey}'.", nameof(settings));

            if (settings.Overrides is { } requested)
            {
                if (requested.DecorationDensityPercent is < 0 or > 200 || !Enum.IsDefined(requested.DecorationPlacementStyle))
                    throw new ArgumentOutOfRangeException(nameof(settings), "Choose a supported prop placement style and a decoration density from 0 to 200 percent.");
                if (!string.IsNullOrWhiteSpace(requested.DecorationProfile) &&
                    !tilesetProfile.DecorationProfiles.ContainsKey(requested.DecorationProfile))
                    throw new ArgumentException($"Tileset '{tilesetProfile.DisplayName}' has no decoration palette '{requested.DecorationProfile}'.", nameof(settings));
            }

            var layoutKey = string.IsNullOrWhiteSpace(settings.LayoutProfileKey)
                ? theme?.LayoutProfileKey ?? string.Empty
                : settings.LayoutProfileKey;
            if (!Catalog.LayoutProfiles.TryGetValue(layoutKey, out var layoutProfile))
                throw new ArgumentException($"Unknown layout profile '{layoutKey}'.", nameof(settings));

            var style = settings.Overrides?.Style ?? layoutProfile.Template.Style;
            ValidateDimensions(settings.Width, settings.Height, theme, style);
            if (settings.Seed is < 0 or > AreaSettingsBounds.MaxSeed)
                throw new ArgumentOutOfRangeException(nameof(settings), "Seed is outside the supported range.");

            if (!_tilesets.TryGetTileset(tilesetProfile.TilesetResref, out var source))
            {
                throw new InvalidOperationException(
                    $"Tileset '{tilesetProfile.TilesetResref}' for profile '{tilesetProfile.DisplayName}' is unavailable.");
            }

            var tileset = source.Model;
            var composition = new DungeonComposition
            {
                Content = theme,
                Tileset = tilesetProfile,
                Layout = layoutProfile
            };
            ValidateEffectiveLayoutSettings(composition, settings);
            var result = GenerationEngine.Generate(
                composition,
                tileset,
                settings.Width,
                settings.Height,
                settings.Seed,
                settings.Overrides,
                LogLayoutDiagnostic);

            return new AreaGenerationDraft(settings, composition, tileset, result, source.Fingerprint);
        }

        /// <summary>
        /// Generates a draft and verifies that all configured encounters can be loaded and placed
        /// before the draft is shown as a creatable preview.
        /// </summary>
        public AreaGenerationDraft Generate(
            AreaGenerationSettings settings,
            IGeneratedAreaBlueprintSource blueprints)
        {
            ArgumentNullException.ThrowIfNull(blueprints);
            var draft = Generate(settings);
            if (draft.Result.Success)
            {
                ValidatePlaceableBlueprints(draft, blueprints);
                GeneratedAreaDocumentPopulator.ValidateEncounterPlacement(draft, blueprints);
            }
            return draft;
        }

        /// <summary>Rejects missing decoration, treasure and transition blueprints before a preview can be created.</summary>
        public static void ValidatePlaceableBlueprints(AreaGenerationDraft draft, IGeneratedAreaBlueprintSource blueprints)
        {
            var content = draft.Composition.Content;
            if (content == null)
                return;

            var required = draft.Result.PlannedDecorations.Select(prop => (Type: ModuleResourceType.Utp, Resref: prop.Resref)).ToList();
            if (draft.Result.Resolved.Rooms.Any(room => room.Role == RoomRole.Boss))
                required.Add((ModuleResourceType.Utp, content.TreasurePlaceableResref));
            foreach (var transition in draft.Result.Resolved.Transitions)
                required.Add(transition.Style == TransitionStyle.Placeable
                    ? (ModuleResourceType.Utp, content.ExitPlaceableResref) : (ModuleResourceType.Utd, content.ExitDoorResref));
            var missing = new List<string>();
            foreach (var resource in required.DistinctBy(item => (item.Type, (item.Resref ?? string.Empty).ToLowerInvariant())))
            {
                if (string.IsNullOrWhiteSpace(resource.Resref) || !blueprints.TryLoadBlueprint(resource.Type, resource.Resref, out _))
                    missing.Add($"{resource.Resref}.{resource.Type.ToString().ToLowerInvariant()}");
            }
            if (missing.Count > 0)
                throw new InvalidOperationException("Cannot create this area: missing prop or door blueprints: " + string.Join(", ", missing) + ".");
        }

        private void LogLayoutDiagnostic(AreaLayoutDiagnostic diagnostic) =>
            _log.Information(
                $"Area layout diagnostic {diagnostic.Code} for tileset {diagnostic.TilesetResref}: {diagnostic.Detail}");

        private static void ValidateTheme(DungeonDetail theme, AreaGenerationSettings settings)
        {
            if (!theme.Tiers.TryGetValue(settings.Tier, out var tier))
            {
                throw new ArgumentException(
                    $"Theme '{theme.DisplayName}' does not define tier {settings.Tier}.",
                    nameof(settings));
            }
            if (string.IsNullOrWhiteSpace(tier.TreasureLootTableId) || tier.TreasureItemCount < 1)
            {
                throw new ArgumentException(
                    $"Theme '{theme.DisplayName}' tier {settings.Tier} has invalid treasure settings.",
                    nameof(settings));
            }
            if (!float.IsFinite(theme.ExitPlaceableFootprintRadius) ||
                !float.IsFinite(theme.ExitDoorFootprintRadius) ||
                !float.IsFinite(theme.TreasurePlaceableFootprintRadius) ||
                theme.ExitPlaceableFootprintRadius <= 0f ||
                theme.ExitDoorFootprintRadius <= 0f ||
                theme.TreasurePlaceableFootprintRadius <= 0f)
            {
                throw new ArgumentException(
                    $"Theme '{theme.DisplayName}' has invalid generated-object footprint settings.",
                    nameof(settings));
            }
            if (tier.Creatures.Count == 0 ||
                tier.Creatures.Any(creature => string.IsNullOrWhiteSpace(creature.Resref) || creature.Weight < 1) ||
                tier.MinCreaturesPerRoom < 0 ||
                tier.MaxCreaturesPerRoom < tier.MinCreaturesPerRoom ||
                string.IsNullOrWhiteSpace(tier.BossResref))
            {
                throw new ArgumentException(
                    $"Theme '{theme.DisplayName}' tier {settings.Tier} has invalid creature settings.",
                    nameof(settings));
            }
        }

        private static void ValidateEffectiveLayoutSettings(
            DungeonComposition composition,
            AreaGenerationSettings settings)
        {
            var parameters = composition.BuildLayoutParameters();
            settings.Overrides?.ApplyTo(parameters, composition.Tileset);
            parameters.Width = settings.Width;
            parameters.Height = settings.Height;
            if (settings.Overrides == null)
                LayoutParameterConstraints.ClampToValid(parameters);
            var bounds = LayoutParameterConstraints.RoomSizeBounds(
                parameters.Style,
                parameters.Width,
                parameters.Height);
            var invalidRoomSizes = parameters.MinRoomCornerSize < bounds.Min ||
                                   parameters.MinRoomCornerSize > bounds.Max ||
                                   parameters.MaxRoomCornerSize < bounds.Min ||
                                   parameters.MaxRoomCornerSize > bounds.Max ||
                                   parameters.MinRoomCornerSize > parameters.MaxRoomCornerSize;
            if (!invalidRoomSizes)
                return;

            throw new ArgumentOutOfRangeException(
                nameof(settings),
                $"Layout settings are outside the safe bounds for {parameters.Style} at " +
                $"{parameters.Width}x{parameters.Height}. Room sizes must be {bounds.Min}-{bounds.Max}, " +
                "and the minimum room size cannot exceed the maximum.");
        }

        private static void ValidateDimensions(
            int width,
            int height,
            DungeonDetail? theme,
            DungeonLayoutStyle style)
        {
            if (width is < AreaSettingsBounds.WidthMin or > AreaSettingsBounds.WidthMax ||
                height is < AreaSettingsBounds.HeightMin or > AreaSettingsBounds.HeightMax)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(width),
                    $"Width and height must each be between {AreaSettingsBounds.WidthMin} and {AreaSettingsBounds.WidthMax}.");
            }

            if (theme != null &&
                (width < theme.MinSize || height < theme.MinSize ||
                 width > theme.MaxSize || height > theme.MaxSize))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(width),
                    $"Theme '{theme.DisplayName}' supports sizes {theme.MinSize}-{theme.MaxSize}.");
            }

            var styleFloor = LayoutStyleSizeFloor.For(style);
            if (width < styleFloor || height < styleFloor)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(width),
                    $"Layout style '{style}' requires width and height of at least {styleFloor}.");
            }
        }
    }
}
