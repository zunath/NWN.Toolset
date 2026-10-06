using Nwn.Authoring.Categories;

namespace Nwn.Toolset.Avalonia.Palettes.Workflow;

/// <summary>Category path helpers shared by the palette's commands.</summary>
public static class PaletteCategoryPaths
{
    /// <summary>
    /// Finds or creates the folder at <paramref name="path"/>, matching each segment case-insensitively,
    /// so a copy lands in the Custom category that corresponds to its source category.
    /// </summary>
    public static CategoryFolder EnsureFolderPath(CategorySection section, IReadOnlyList<string> path)
    {
        ArgumentNullException.ThrowIfNull(section);
        ArgumentNullException.ThrowIfNull(path);
        if (path.Count == 0)
            throw new ArgumentException("A category path needs at least one segment.", nameof(path));

        var current = section.Folders.FirstOrDefault(folder =>
                          string.Equals(folder.Name, path[0], StringComparison.OrdinalIgnoreCase))
                      ?? section.AddFolder(path[0]);

        for (var index = 1; index < path.Count; index++)
        {
            var segment = path[index];
            current = current.Children.FirstOrDefault(child =>
                          string.Equals(child.Name, segment, StringComparison.OrdinalIgnoreCase))
                      ?? current.AddChild(segment);
        }

        return current;
    }

    /// <summary>
    /// The folder an entry is copied from: the first filing beneath <paramref name="selectedFolder"/>
    /// when there is one (a parent row lists all its descendants), otherwise the entry's first filing.
    /// Null when the entry is unfiled.
    /// </summary>
    public static CategoryFolder? SourceFolder(
        CategorySection? section,
        string resRef,
        CategoryFolder? selectedFolder)
    {
        if (section == null)
            return null;

        var containing = section.FoldersContaining(resRef).ToList();
        if (containing.Count == 0)
            return null;

        if (selectedFolder is not null)
        {
            var selectedPath = section.PathTo(selectedFolder);
            var beneathSelection = containing.FirstOrDefault(folder =>
            {
                var candidatePath = section.PathTo(folder);
                return candidatePath.Count >= selectedPath.Count &&
                       candidatePath.Take(selectedPath.Count)
                           .SequenceEqual(selectedPath, StringComparer.OrdinalIgnoreCase);
            });

            if (beneathSelection != null)
                return beneathSelection;
        }

        return containing[0];
    }
}
