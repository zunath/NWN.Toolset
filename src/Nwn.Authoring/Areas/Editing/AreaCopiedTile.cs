using Nwn.Authoring.Documents.NimGff;

namespace Nwn.Authoring.Areas.Editing;

public sealed record AreaCopiedTile(int Column, int Row, JsonGffStruct Tile);
