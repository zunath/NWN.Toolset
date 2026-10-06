using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Nwn.Authoring.Categories;

namespace Nwn.Toolset.Avalonia.Explorer;

/// <summary>One destination on the "Move to" submenu: a folder, labelled by its full path.</summary>
/// <remarks>
/// A submenu of folders rather than a modal picker, because moving one thing into a folder should cost
/// one gesture, and the tree the builder is looking at is already the list of choices. The label is the
/// whole path since folder names repeat between branches. It carries its own command: a styled
/// <c>MenuItem</c> generated from an ItemsSource can bind off the item but has no route back to the
/// panel's own commands.
/// </remarks>
public sealed class ExplorerMoveTarget
{
    public ExplorerMoveTarget(CategoryFolder folder, string path, Action<CategoryFolder> move)
    {
        ArgumentNullException.ThrowIfNull(move);
        Folder = folder ?? throw new ArgumentNullException(nameof(folder));
        Path = path;
        Command = new RelayCommand(() => move(folder));
    }

    public CategoryFolder Folder { get; }

    public string Path { get; }

    public ICommand Command { get; }
}
