using Nwn.Authoring.Resources;
using Nwn.Toolset.Avalonia.Palettes;
using Nwn.Toolset.Avalonia.Palettes.Workflow;

namespace Nwn.Toolset.Avalonia.Tests.Support;

internal sealed class FakePaletteBlueprintOperations(FakePaletteContentSource content) : IPaletteBlueprintOperations
{
    public HashSet<ModuleResourceType> Creatable { get; } = new() { ModuleResourceType.Utc, ModuleResourceType.Utp };

    public HashSet<string> OpenInEditor { get; } = new(StringComparer.OrdinalIgnoreCase);

    public List<string> OpenedEditors { get; } = new();

    public List<(ModuleResourceType Type, string ResRef, string Name)> Created { get; } = new();

    public List<(PaletteSource Source, string ResRef)> Copied { get; } = new();

    public List<FakePaletteBlueprintDeletion> Deletions { get; } = new();

    public List<CancellationToken> WriteTokens { get; } = new();

    public string? CommitProblem { get; set; }

    public bool Deletable { get; set; } = true;

    /// <summary>When set, Edit Copy hands the source to an editor's copy mode instead of writing a copy.</summary>
    public bool CopiesInEditor { get; set; }

    /// <summary>
    /// When set, every create and copy waits for this before it writes, the way a host's asynchronous
    /// write does; the wait honours the caller's cancellation.
    /// </summary>
    public Task? WriteGate { get; set; }

    public bool CanCreate(ModuleResourceType type) => Creatable.Contains(type);

    public bool CanDelete(ModuleResourceType type) => Deletable;

    public async Task<PaletteBlueprintCreation> CreateAsync(
        ModuleResourceType type,
        string resRef,
        string name,
        CancellationToken cancellationToken)
    {
        WriteTokens.Add(cancellationToken);
        if (WriteGate is { } gate)
            await gate.WaitAsync(cancellationToken).ConfigureAwait(true);

        if (content.CustomResRefs(type).Contains(resRef))
            return PaletteBlueprintCreation.AlreadyExists();

        Created.Add((type, resRef, name));
        content.AddCustom(type, resRef, name);
        return PaletteBlueprintCreation.Created("module/" + resRef);
    }

    public async Task<PaletteBlueprintCopy> CopyAsync(
        ModuleResourceType type,
        PaletteSource source,
        string resRef,
        CancellationToken cancellationToken)
    {
        WriteTokens.Add(cancellationToken);
        if (WriteGate is { } gate)
            await gate.WaitAsync(cancellationToken).ConfigureAwait(true);

        Copied.Add((source, resRef));
        if (CopiesInEditor)
            return PaletteBlueprintCopy.OpenedInEditor();

        var copy = resRef + "001";
        content.AddCustom(type, copy);
        return PaletteBlueprintCopy.Copied(copy, "module/" + copy);
    }

    public PaletteBlueprintDeletePreparation PrepareDelete(ModuleResourceType type, string resRef)
    {
        var deletion = new FakePaletteBlueprintDeletion(this, content, type, resRef);
        Deletions.Add(deletion);
        return PaletteBlueprintDeletePreparation.Ready(deletion);
    }

    public bool IsOpenInEditor(ModuleResourceType type, string resRef) => OpenInEditor.Contains(resRef);

    public void OpenEditor(ModuleResourceType type, string resRef) => OpenedEditors.Add(resRef);
}
