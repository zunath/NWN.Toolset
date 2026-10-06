using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Resources;

namespace Nwn.Toolset.Avalonia.Areas.Properties;

/// <summary>The blueprint palette tree an instance section's Add flow browses.</summary>
public interface IAreaInstancePaletteSource
{
    /// <summary>
    /// Finds the palette for <paramref name="type"/>. <paramref name="source"/> names where it was
    /// looked for, in both outcomes, for the log.
    /// </summary>
    bool TryLocate(ModuleResourceType type, out string source);

    /// <summary>Reads the palette's root nodes; throws when the palette is unreadable.</summary>
    IReadOnlyList<PaletteNode> Read(string source);
}
