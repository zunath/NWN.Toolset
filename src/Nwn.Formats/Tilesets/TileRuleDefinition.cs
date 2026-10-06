namespace Nwn.Formats.Tilesets
{

    /// <summary>
    /// One entry from a [PRIMARY RULES]/[SECONDARY RULES] block describing how the toolset
    /// auto-terrains a newly placed tile against its neighbors.
    /// </summary>
    public sealed record TileRuleDefinition(
        string Placed,
        int PlacedHeight,
        string Adjacent,
        int AdjacentHeight,
        string Changed,
        int ChangedHeight);

}
