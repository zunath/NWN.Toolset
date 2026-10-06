using Nwn.Authoring.Resources;

namespace Nwn.Toolset.Avalonia.Palettes.Workflow;

/// <summary>
/// Resolves entry display names for one snapshot, falling back to the resref while the host has no name.
/// </summary>
/// <remarks>
/// Base-game blueprints are not in the module, so the only name they have is the one their palette file
/// declares; module blueprints are named by the host's catalog.
/// </remarks>
internal sealed class PaletteEntryNames
{
    private readonly IPaletteContentSource _content;
    private readonly ModuleResourceType _type;
    private readonly IReadOnlyDictionary<string, string>? _standardNames;

    public PaletteEntryNames(IPaletteContentSource content, ModuleResourceType type, PaletteSource source)
    {
        _content = content ?? throw new ArgumentNullException(nameof(content));
        _type = type;
        _standardNames = source == PaletteSource.Standard ? content.Standard(type).Names : null;
    }

    public string NameFor(string resRef)
    {
        if (_standardNames is not null)
            return _standardNames.TryGetValue(resRef, out var standardName) ? standardName : resRef;

        return _content.CustomName(_type, resRef) ?? resRef;
    }
}
