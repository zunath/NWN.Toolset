namespace Nwn.Formats.Tilesets
{

    /// <summary>
    /// One entry from a [CROSSER TYPES] block: a named feature (bridge, doorway, ramp, ...) a
    /// tile edge can carry between two terrains.
    /// </summary>
    public sealed record CrosserDefinition(string Name, int? StrRef, string? UnlocalizedName);

}
