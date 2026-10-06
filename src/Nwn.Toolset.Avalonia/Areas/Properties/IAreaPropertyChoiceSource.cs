using Nwn.Authoring.Areas.Properties;
using Nwn.Toolset.Avalonia.Fields;

namespace Nwn.Toolset.Avalonia.Areas.Properties;

/// <summary>
/// Game-data choices for native area properties, such as sky boxes and load screens from the host's
/// 2DA and TLK sources.
/// </summary>
public interface IAreaPropertyChoiceSource
{
    /// <summary>
    /// The choices for <paramref name="field"/>, or null to keep it a plain number. An empty list keeps
    /// the dropdown but shows the stored value read-only, as for an unavailable table.
    /// </summary>
    IReadOnlyList<LookupOption>? ChoicesFor(AreaPropertyFieldId field);
}
