using Nwn.Authoring.Resources;
using Nwn.Toolset.Avalonia.Areas;
using Nwn.Toolset.Avalonia.Explorer.Workflow;
using Nwn.Toolset.Avalonia.Palettes.Workflow;

namespace Nwn.Toolset.Avalonia.Tests.Support;

/// <summary>Creates resources in the fake module; areas through a form, everything else through a prompt.</summary>
internal sealed class FakeExplorerCreation : IModuleExplorerCreation
{
    private readonly FakeExplorerContentSource _content;

    public FakeExplorerCreation(FakeExplorerContentSource content)
    {
        _content = content;
    }

    public ModuleExplorerCreationOptions? Options { get; set; } = ModuleExplorerCreationOptions.None;

    public string? CreateFailure { get; set; }

    public List<(ModuleResourceType Type, string ResRef, string Name, ModuleExplorerCreationOptions Options)> Writes { get; } = new();

    public List<(ModuleResourceType Type, string ResRef)> CreatedNotices { get; } = new();

    public ModuleExplorerFormCallbacks? FormCallbacks { get; private set; }

    public Action? DuringOptions { get; set; }

    public bool CanCreate(ModuleResourceType type) => true;

    public ModuleExplorerCreationMode Mode(ModuleResourceType type) =>
        type == ModuleResourceType.Area ? ModuleExplorerCreationMode.Form : ModuleExplorerCreationMode.NamePrompt;

    public string ToResRef(ModuleResourceType type, string name) =>
        new string(name.Trim().ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray()).Trim('_');

    public bool Exists(ModuleResourceType type, string resRef) => _content.Exists(type, resRef);

    public Task<ModuleExplorerCreationOptions?> ChooseOptionsAsync(ModuleResourceType type)
    {
        DuringOptions?.Invoke();
        return Task.FromResult(Options);
    }

    public PaletteOperationResult Create(
        ModuleResourceType type, string resRef, string name, ModuleExplorerCreationOptions options)
    {
        if (CreateFailure is { } failure)
            return PaletteOperationResult.Failed(failure);

        Writes.Add((type, resRef, name, options));
        _content.Add(type, resRef, name);
        return PaletteOperationResult.Ok();
    }

    public IAreaCreationFormState? OpenForm(ModuleResourceType type, ModuleExplorerFormCallbacks callbacks)
    {
        FormCallbacks = callbacks;
        return new AreaCreationFormState();
    }

    public void Created(ModuleResourceType type, string resRef) => CreatedNotices.Add((type, resRef));

    /// <summary>Stands in for the form's Create button.</summary>
    public void CompleteForm(string resRef)
    {
        _content.Add(ModuleResourceType.Area, resRef);
        FormCallbacks!.Created(resRef);
    }
}
