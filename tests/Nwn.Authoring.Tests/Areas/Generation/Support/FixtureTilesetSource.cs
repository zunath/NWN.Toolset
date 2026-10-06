using Nwn.Authoring.Areas.Generation.Hosting;
using Nwn.Authoring.Areas.Generation.Tilesets;

namespace Nwn.Authoring.Tests.Areas.Generation.Support;

/// <summary>Serves one in-memory tileset to the generator.</summary>
internal sealed class FixtureTilesetSource : IAreaGenerationTilesetSource
{
    private readonly TilesetModel _model;
    private readonly string _fingerprint;

    public FixtureTilesetSource(TilesetModel model, string fingerprint)
    {
        _model = model;
        _fingerprint = fingerprint;
    }

    public IReadOnlyCollection<string> GetTilesetResRefs() => [_model.Resref];

    public bool TryGetTileset(string tilesetResRef, out GeneratorTileset tileset)
    {
        var found = string.Equals(tilesetResRef, _model.Resref, StringComparison.OrdinalIgnoreCase);
        tileset = found ? new GeneratorTileset(_model, _fingerprint) : null!;
        return found;
    }
}
