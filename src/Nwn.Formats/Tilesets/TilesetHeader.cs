namespace Nwn.Formats.Tilesets
{
    /// <summary>
    /// Just the identifying fields of a tileset's [GENERAL] block, for labelling a tileset picker
    /// without parsing the whole file (see <see cref="SetFileParser.ParseHeader"/>).
    /// </summary>
    /// <param name="Name">The tileset's internal name, usually the ResRef in caps ("ZTD01").</param>
    /// <param name="UnlocalizedName">The human-readable name ("[CEP] Desert"), absent in some tilesets.</param>
    /// <param name="DisplayNameStrRef">Strref for a localized name, or -1 when not declared.</param>
    public sealed record TilesetHeader(string Name, string UnlocalizedName, int DisplayNameStrRef);

}
