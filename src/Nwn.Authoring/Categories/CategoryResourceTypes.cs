using Nwn.Authoring.Resources;

namespace Nwn.Authoring.Categories;

/// <summary>Maps native module resource kinds to the extension keys used in category sidecars.</summary>
internal static class CategoryResourceTypes
{
    public static string Extension(ModuleResourceType type) => type switch
    {
        ModuleResourceType.Area => "are",
        ModuleResourceType.Utc => "utc",
        ModuleResourceType.Uti => "uti",
        ModuleResourceType.Utp => "utp",
        ModuleResourceType.Utd => "utd",
        ModuleResourceType.Utm => "utm",
        ModuleResourceType.Utt => "utt",
        ModuleResourceType.Uts => "uts",
        ModuleResourceType.Utw => "utw",
        ModuleResourceType.Dlg => "dlg",
        ModuleResourceType.Nss => "nss",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown module resource type.")
    };

    public static bool TryFromExtension(string? extension, out ModuleResourceType type)
    {
        foreach (var candidate in Enum.GetValues<ModuleResourceType>())
        {
            if (string.Equals(Extension(candidate), extension, StringComparison.OrdinalIgnoreCase))
            {
                type = candidate;
                return true;
            }
        }

        type = default;
        return false;
    }
}
