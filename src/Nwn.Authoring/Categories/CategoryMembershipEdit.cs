using Nwn.Authoring.Resources;

namespace Nwn.Authoring.Categories;

/// <summary>
/// One refile of a resource between folders of a category section, recorded so it can be undone and
/// redone. Folders are named by <see cref="CategorySection.PathKey"/>, never by reference, because a
/// rebuilt tree holds new folder objects and a renamed or deleted folder must make the edit unreplayable.
/// </summary>
/// <param name="Type">The section the resource belongs to.</param>
/// <param name="ResRef">The resource that moved.</param>
/// <param name="DisplayName">What status lines call the resource.</param>
/// <param name="BeforeFolderPaths">Every folder that held the resource before the move; empty for Unsorted.</param>
/// <param name="AfterFolderPaths">Every folder that holds it after the move; empty for Unsorted.</param>
public sealed record CategoryMembershipEdit(
    ModuleResourceType Type,
    string ResRef,
    string DisplayName,
    IReadOnlyList<string> BeforeFolderPaths,
    IReadOnlyList<string> AfterFolderPaths);
