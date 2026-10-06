using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Resources;

namespace Nwn.Toolset.Avalonia.Areas.Properties;

/// <summary>The blueprints an instance section creates placements from.</summary>
public interface IAreaInstanceBlueprintSource
{
    /// <summary>
    /// Identifies the module the sections edit; a copied placement is only pasted into an area of the
    /// same module.
    /// </summary>
    string ModuleIdentity { get; }

    /// <summary>
    /// Loads a blueprint. <paramref name="useIndexedBlueprint"/> asks for the host's indexed copy
    /// (for example, a palette entry outside the module's own files); throws when it cannot be read.
    /// </summary>
    JsonGffDocument LoadBlueprint(ModuleResourceType type, string resRef, bool useIndexedBlueprint);
}
