using Nwn.Authoring.Areas.Properties;

namespace Nwn.Toolset.Avalonia.Areas.Properties;

/// <summary>Which native area properties a host leaves out of the page or shows read-only.</summary>
/// <param name="Excluded">Fields the host edits elsewhere, such as an area name box above the page.</param>
/// <param name="ReadOnly">Fields shown but not editable, beyond the catalog's own read-only identity fields.</param>
public sealed record AreaPropertyFieldPolicy(
    IReadOnlySet<AreaPropertyFieldId> Excluded,
    IReadOnlySet<AreaPropertyFieldId> ReadOnly)
{
    /// <summary>Every field, editable as the catalog declares.</summary>
    public static AreaPropertyFieldPolicy Default { get; } =
        new(new HashSet<AreaPropertyFieldId>(), new HashSet<AreaPropertyFieldId>());
}
