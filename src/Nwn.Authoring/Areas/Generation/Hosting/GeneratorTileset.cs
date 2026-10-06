using Nwn.Authoring.Areas.Generation.Tilesets;

namespace Nwn.Authoring.Areas.Generation.Hosting;

/// <summary>A solver-ready tileset and the host's token for the exact source it was read from.</summary>
/// <param name="Model">The parsed tileset.</param>
/// <param name="Fingerprint">
/// Identifies the source version (for example a content hash), or empty when the host does not track one.
/// A draft keeps it so the host's writer can refuse to create an area from a tileset that changed afterward.
/// </param>
public sealed record GeneratorTileset(TilesetModel Model, string Fingerprint);
