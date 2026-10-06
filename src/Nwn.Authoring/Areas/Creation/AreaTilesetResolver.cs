using Nwn.Formats.Tilesets;

namespace Nwn.Authoring.Areas.Creation;

public delegate bool AreaTilesetResolver(string resRef, out TilesetDefinition tileset);
