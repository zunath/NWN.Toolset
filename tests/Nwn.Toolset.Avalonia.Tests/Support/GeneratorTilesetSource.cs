using Nwn.Authoring.Areas.Generation.Hosting;
using Nwn.Authoring.Areas.Generation.Tilesets;

namespace Nwn.Toolset.Avalonia.Tests.Support;

/// <summary>Serves one complete flat-cornered tileset to the Area Generator.</summary>
public sealed class GeneratorTilesetSource : IAreaGenerationTilesetSource
{
    public const string ResRef = "fixture";

    public IReadOnlyCollection<string> GetTilesetResRefs() => [ResRef];

    public bool TryGetTileset(string tilesetResRef, out GeneratorTileset tileset)
    {
        var found = string.Equals(tilesetResRef, ResRef, StringComparison.OrdinalIgnoreCase);
        tileset = found ? new GeneratorTileset(Create(), "fixture-hash") : null!;
        return found;
    }

    private static TilesetModel Create()
    {
        var model = new TilesetModel { Resref = ResRef, DefaultTerrain = "Solid", FloorTerrain = "Floor" };
        for (var mask = 0; mask < 16; mask++)
        {
            var corners = new string[4];
            for (var slot = 0; slot < corners.Length; slot++)
                corners[slot] = (mask & (1 << slot)) == 0 ? "Solid" : "Floor";
            model.Tiles.Add(new TileRecord
            {
                TileId = mask,
                Corners = corners,
                CornerHeights = [0, 0, 0, 0],
                Edges = ["", "", "", ""]
            });
        }

        return model;
    }
}
