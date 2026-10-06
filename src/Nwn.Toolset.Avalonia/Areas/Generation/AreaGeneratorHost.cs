using Nwn.Authoring.Areas.Generation.Hosting;
using Nwn.Toolset.Avalonia.Localization;

namespace Nwn.Toolset.Avalonia.Areas.Generation;

/// <summary>
/// Everything a host supplies to the Area Generator. The catalog, tilesets and writer are required;
/// every other service is optional and the generator degrades without it: no blueprints means a theme
/// with objects cannot be validated, no tile graphics means Map graphics mode draws schematic colors.
/// </summary>
/// <param name="Catalog">The themes, tileset profiles and layout profiles the pickers list.</param>
/// <param name="Tilesets">The tilesets available to solve against.</param>
/// <param name="Writer">The host's transaction for creating the previewed area in its open module.</param>
public sealed record AreaGeneratorHost(
    IAreaGenerationCatalog Catalog,
    IAreaGenerationTilesetSource Tilesets,
    IGeneratedAreaWriter Writer)
{
    /// <summary>Creature, door and placeable blueprints for generated objects.</summary>
    public IGeneratedAreaBlueprintSource? Blueprints { get; init; }

    /// <summary>Game-specific adjustments to placed instances.</summary>
    public IGeneratedAreaPopulationPolicy? PopulationPolicy { get; init; }

    /// <summary>Tile artwork for the Map graphics preview.</summary>
    public IAreaPreviewTileGraphics? TileGraphics { get; init; }

    /// <summary>The host's diagnostic log.</summary>
    public IAreaGenerationLog? Log { get; init; }

    /// <summary>How heavy phases leave the UI thread, or null for the thread pool.</summary>
    public IAreaGeneratorBackgroundTaskRunner? BackgroundTasks { get; init; }

    /// <summary>The generator's text, or null for English.</summary>
    public AreaGeneratorTexts? Texts { get; init; }

    /// <summary>How the generator window presents itself, or null for the defaults.</summary>
    public AreaGeneratorWindowOptions? Window { get; init; }
}
