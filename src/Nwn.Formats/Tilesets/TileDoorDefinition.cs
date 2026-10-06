namespace Nwn.Formats.Tilesets
{

    /// <summary>
    /// A door placement on a tile, from a [TILEnDOORd] block.
    /// </summary>
    public sealed record TileDoorDefinition(int Type, double X, double Y, double Z, double Orientation);

}
