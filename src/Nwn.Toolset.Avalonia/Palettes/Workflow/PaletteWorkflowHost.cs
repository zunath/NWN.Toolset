namespace Nwn.Toolset.Avalonia.Palettes.Workflow;

/// <summary>
/// Everything a host supplies to <see cref="PaletteWorkflowController"/>. Content and categories are
/// required; every other service is optional and the palette degrades the way it always has without it:
/// no prompts means no create, rename or delete; no previews means glyph tiles; no tilesets means Tiles
/// mode reports that the tileset could not be loaded.
/// </summary>
/// <param name="Content">The host's blueprints and type names.</param>
/// <param name="Categories">The host's category sidecar.</param>
public sealed record PaletteWorkflowHost(IPaletteContentSource Content, IPaletteCategoryStore Categories)
{
    /// <summary>Blueprint create, copy, delete and open-editor operations.</summary>
    public IPaletteBlueprintOperations? Blueprints { get; init; }

    /// <summary>
    /// The host's own blueprint creation UI, used instead of the name prompt for the types it handles.
    /// </summary>
    public IPaletteBlueprintCreationDialog? CreationDialog { get; init; }

    /// <summary>Text prompts and destructive confirmations.</summary>
    public IPalettePrompts? Prompts { get; init; }

    /// <summary>Type icons and rendered previews.</summary>
    public IPalettePreviewSource? Previews { get; init; }

    /// <summary>The area in front, for placement and for Tiles mode's tileset.</summary>
    public IPalettePlacementTargetProvider? Placement { get; init; }

    /// <summary>Tileset palettes for Tiles mode.</summary>
    public IPaletteTilesetSource? Tilesets { get; init; }

    /// <summary>Persisted panel preferences.</summary>
    public IPaletteSettings? Settings { get; init; }

    /// <summary>The module-wide write lock.</summary>
    public IPaletteWriteGate? WriteGate { get; init; }

    /// <summary>The host's output log.</summary>
    public IPaletteLog? Log { get; init; }

    /// <summary>Text for the shared palette view, or null for English.</summary>
    public PaletteTexts? PaletteTexts { get; init; }

    /// <summary>Text for the workflow's own labels and messages, or null for English.</summary>
    public PaletteWorkflowTexts? WorkflowTexts { get; init; }
}
