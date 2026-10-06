namespace Nwn.Toolset.Avalonia.Areas;

/// <summary>A host-provided tileset option displayed by the area creation form.</summary>
public sealed record AreaTilesetChoice(string ResRef, string Label)
{
    public override string ToString() => Label;
}
