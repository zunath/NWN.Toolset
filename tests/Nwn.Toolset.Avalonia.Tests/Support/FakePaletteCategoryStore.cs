using Nwn.Authoring.Categories;
using Nwn.Authoring.Resources;
using Nwn.Toolset.Avalonia.Palettes.Workflow;

namespace Nwn.Toolset.Avalonia.Tests.Support;

/// <summary>An in-memory sidecar that, like a real host, restores the persisted tree after a refused save.</summary>
internal sealed class FakePaletteCategoryStore : IPaletteCategoryStore
{
    private CategoryCatalog _persisted = new();

    public event Action? Changed;

    public CategoryCatalog Live { get; private set; } = new();

    public bool IsModuleOpen { get; set; } = true;

    public string? Refusal { get; set; }

    public int Saves { get; private set; }

    public CategorySection? Section(ModuleResourceType type) => IsModuleOpen ? Live.Section(type) : null;

    public PaletteCategorySaveResult CanSaveChanges() =>
        Refusal is null ? PaletteCategorySaveResult.Ok() : PaletteCategorySaveResult.Failed(Refusal);

    public PaletteCategorySaveResult SaveChanges()
    {
        if (Refusal is not null)
        {
            Live = _persisted.DeepClone();
            return PaletteCategorySaveResult.Failed(Refusal);
        }

        _persisted = Live.DeepClone();
        Saves++;
        Changed?.Invoke();
        return PaletteCategorySaveResult.Ok();
    }

    /// <summary>
    /// Replaces the live tree with a fresh copy of the persisted one, as a host does when it reloads the
    /// sidecar or repairs placeholder names: every folder object the palette held is gone.
    /// </summary>
    public void ReplaceLive(Action<CategoryCatalog>? edit = null)
    {
        var replacement = _persisted.DeepClone();
        edit?.Invoke(replacement);
        _persisted = replacement.DeepClone();
        Live = replacement;
        Changed?.Invoke();
    }

    /// <summary>Accepts the current tree as persisted without counting a save.</summary>
    public void Commit() => _persisted = Live.DeepClone();
}
