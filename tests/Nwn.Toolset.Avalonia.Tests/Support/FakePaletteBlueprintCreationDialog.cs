using Nwn.Authoring.Resources;
using Nwn.Toolset.Avalonia.Palettes.Workflow;

namespace Nwn.Toolset.Avalonia.Tests.Support;

/// <summary>A host's own creation UI: writes whatever <see cref="Result"/> names, or reports a cancel.</summary>
internal sealed class FakePaletteBlueprintCreationDialog(FakePaletteContentSource content) : IPaletteBlueprintCreationDialog
{
    public HashSet<ModuleResourceType> Handled { get; } = new() { ModuleResourceType.Utp };

    public List<ModuleResourceType> Shown { get; } = new();

    public PaletteBlueprintCreation Result { get; set; } = PaletteBlueprintCreation.Cancelled();

    public bool Handles(ModuleResourceType type) => Handled.Contains(type);

    public Task<PaletteBlueprintCreation> CreateAsync(ModuleResourceType type, CancellationToken cancellationToken)
    {
        Shown.Add(type);
        if (Result is { Outcome: PaletteBlueprintCreationOutcome.Created, ResRef: { } resRef })
            content.AddCustom(type, resRef, Result.Name);
        return Task.FromResult(Result);
    }
}
