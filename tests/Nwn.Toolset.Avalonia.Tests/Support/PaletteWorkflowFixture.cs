using Nwn.Toolset.Avalonia.Palettes;
using Nwn.Toolset.Avalonia.Palettes.Workflow;

namespace Nwn.Toolset.Avalonia.Tests.Support;

/// <summary>A palette workflow wired to in-memory fakes for every host service.</summary>
internal sealed class PaletteWorkflowFixture : IDisposable
{
    private PaletteWorkflowController? _controller;

    public PaletteWorkflowFixture()
    {
        Blueprints = new FakePaletteBlueprintOperations(Content);
    }

    public FakePaletteContentSource Content { get; } = new();

    public FakePaletteCategoryStore Categories { get; } = new();

    public FakePaletteBlueprintOperations Blueprints { get; }

    public FakePalettePrompts Prompts { get; } = new();

    public FakePaletteSettings Settings { get; } = new();

    public FakePaletteWriteGate WriteGate { get; } = new();

    public FakePalettePlacement Placement { get; } = new();

    public FakePaletteTilesetSource Tilesets { get; } = new();

    public FakePaletteLog Log { get; } = new();

    /// <summary>Optional, set before <see cref="Controller"/> is first used.</summary>
    public FakePalettePreviewSource? Previews { get; set; }

    /// <summary>Optional, set before <see cref="Controller"/> is first used.</summary>
    public FakePaletteBlueprintCreationDialog? CreationDialog { get; set; }

    public PaletteWorkflowController Controller => _controller ??= new PaletteWorkflowController(
        new PaletteWorkflowHost(Content, Categories)
        {
            Blueprints = Blueprints,
            CreationDialog = CreationDialog,
            Prompts = Prompts,
            Previews = Previews,
            Placement = Placement,
            Tilesets = Tilesets,
            Settings = Settings,
            WriteGate = WriteGate,
            Log = Log
        });

    public PalettePresentationState State => Controller.PresentationState;

    /// <summary>Selects the category row with this name, expanding collapsed parents until it is visible.</summary>
    public PaletteCategoryRow SelectRow(string name)
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var row = State.Rows.FirstOrDefault(candidate => candidate.Name == name);
            if (row is not null)
            {
                State.SelectedRow = row;
                return row;
            }

            foreach (var collapsed in State.Rows.Where(candidate => candidate.HasChildren && !candidate.IsExpanded).ToList())
                State.ToggleExpandCommand.Execute(collapsed);
        }

        throw new InvalidOperationException($"No category row named '{name}'.");
    }

    /// <summary>The visible tile for a resref, after selecting the row it is filed under.</summary>
    public PaletteEntryRow Tile(string resRef) =>
        State.Tiles.FirstOrDefault(tile => tile.ResRef == resRef)
        ?? throw new InvalidOperationException($"No tile for '{resRef}'.");

    public void Dispose() => _controller?.Dispose();
}
