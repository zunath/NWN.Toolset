namespace Nwn.Authoring.Categories;

/// <summary>
/// Refiles a resource within one category section: out of every folder that holds it and into exactly
/// the destinations asked for. A resref may legally sit in several folders, but a move the builder asked
/// for means those destinations, not extra ones.
/// </summary>
public static class CategoryMembershipMoves
{
    /// <summary>The path keys of every folder that currently holds <paramref name="resRef"/>.</summary>
    public static IReadOnlyList<string> FolderPathsContaining(CategorySection section, string resRef)
    {
        ArgumentNullException.ThrowIfNull(section);
        return section.FoldersContaining(resRef).Select(section.PathKey).ToArray();
    }

    /// <summary>
    /// Moves a resource into one folder, or to Unsorted when <paramref name="target"/> is null.
    /// </summary>
    /// <returns>True when any membership changed.</returns>
    public static bool MoveTo(CategorySection section, string resRef, CategoryFolder? target)
    {
        ArgumentNullException.ThrowIfNull(section);
        var changed = false;
        foreach (var folder in section.AllFolders())
            changed |= folder.RemoveMember(resRef);

        if (target != null)
            changed |= target.AddMember(resRef);

        return changed;
    }

    /// <summary>
    /// Resolves stored folder paths against the current tree. Fails with the first path that no longer
    /// names a folder, because replaying a move into a folder that is gone would lose the resource.
    /// </summary>
    public static bool TryResolve(
        CategorySection section,
        IReadOnlyList<string> folderPaths,
        out IReadOnlyList<CategoryFolder> folders,
        out string? missingPath)
    {
        ArgumentNullException.ThrowIfNull(section);
        ArgumentNullException.ThrowIfNull(folderPaths);
        var resolved = new List<CategoryFolder>();
        foreach (var path in folderPaths)
        {
            var folder = section.FindByPathKey(path);
            if (folder == null)
            {
                folders = Array.Empty<CategoryFolder>();
                missingPath = path;
                return false;
            }

            resolved.Add(folder);
        }

        folders = resolved;
        missingPath = null;
        return true;
    }

    /// <summary>Makes <paramref name="destinations"/> exactly the folders that hold the resource.</summary>
    public static void Apply(CategorySection section, string resRef, IEnumerable<CategoryFolder> destinations)
    {
        ArgumentNullException.ThrowIfNull(section);
        ArgumentNullException.ThrowIfNull(destinations);
        foreach (var folder in section.AllFolders())
            folder.RemoveMember(resRef);
        foreach (var folder in destinations)
            folder.AddMember(resRef);
    }
}
