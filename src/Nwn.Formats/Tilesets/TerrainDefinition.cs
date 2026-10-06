namespace Nwn.Formats.Tilesets
{
    /// <summary>
    /// One entry from a [TERRAIN TYPES] block: a named ground surface a tile corner can carry.
    /// </summary>
    public sealed record TerrainDefinition(string Name, int? StrRef, string? UnlocalizedName);

}
