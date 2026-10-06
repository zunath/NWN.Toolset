namespace Nwn.Authoring.Areas.Generation.Hosting;

/// <summary>The host's tile artwork for the Map graphics preview.</summary>
public interface IAreaPreviewTileGraphics
{
    /// <summary>Loads a tile's 2D map picture by name, or returns false so the preview falls back to schematic colors.</summary>
    bool TryLoadTileImage(string imageMap2D, out AreaPreviewTexture texture);
}
