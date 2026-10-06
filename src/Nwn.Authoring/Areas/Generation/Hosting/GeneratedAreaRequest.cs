using Nwn.Authoring.Areas.Creation;
using Nwn.Authoring.Areas.Generation.Drafting;

namespace Nwn.Authoring.Areas.Generation.Hosting;

/// <summary>Everything a host's writer needs to create one previewed draft as a new area.</summary>
/// <param name="Draft">The solved draft the preview showed.</param>
/// <param name="ResRef">The canonical (trimmed, lowercase) ResRef for the new area.</param>
/// <param name="DisplayName">The new area's display name.</param>
/// <param name="Populate">
/// Writes the draft's tiles, lighting, atmosphere, transitions, dressing and encounters into the new
/// area's documents. The host's writer calls it inside its own transaction after initializing them.
/// </param>
public sealed record GeneratedAreaRequest(
    AreaGenerationDraft Draft,
    string ResRef,
    string DisplayName,
    AreaDocumentPopulator Populate);
