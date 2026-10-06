using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Resources;
using Nwn.Toolset.Avalonia.Areas.Properties;

namespace Nwn.Toolset.Avalonia.Tests.Support;

internal sealed class FakeAreaInstancePaletteSource : IAreaInstancePaletteSource
{
    public Dictionary<ModuleResourceType, IReadOnlyList<PaletteNode>> Palettes { get; } = new();

    public Exception? ReadFailure { get; set; }

    public bool TryLocate(ModuleResourceType type, out string source)
    {
        source = $"palette/{type}";
        return Palettes.ContainsKey(type);
    }

    public IReadOnlyList<PaletteNode> Read(string source)
    {
        if (ReadFailure != null)
            throw ReadFailure;

        return Palettes.Single(pair => $"palette/{pair.Key}" == source).Value;
    }
}
