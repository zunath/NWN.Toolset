namespace Nwn.Toolset.Avalonia.Palettes;

/// <summary>A category search result with its parent path.</summary>
public sealed record PaletteCategoryMatch(PaletteCategoryId Id, string Name, string ParentPath, int Count)
{
    public bool HasParentPath => !string.IsNullOrEmpty(ParentPath);
}
