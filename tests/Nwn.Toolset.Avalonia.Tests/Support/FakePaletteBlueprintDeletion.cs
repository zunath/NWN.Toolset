using Nwn.Authoring.Resources;
using Nwn.Toolset.Avalonia.Palettes.Workflow;

namespace Nwn.Toolset.Avalonia.Tests.Support;

internal sealed class FakePaletteBlueprintDeletion(
    FakePaletteBlueprintOperations operations,
    FakePaletteContentSource content,
    ModuleResourceType type,
    string resRef) : IPaletteBlueprintDeletion
{
    public string DisplayName => resRef + ".file";

    public string Location => "module/" + resRef;

    public bool IsCurrent { get; set; } = true;

    public bool Committed { get; private set; }

    public bool Disposed { get; private set; }

    public PaletteOperationResult Commit()
    {
        if (operations.CommitProblem is { } problem)
            return PaletteOperationResult.Failed(problem);

        Committed = true;
        content.Custom[type].Remove(resRef);
        return PaletteOperationResult.Ok();
    }

    public void Dispose() => Disposed = true;
}
