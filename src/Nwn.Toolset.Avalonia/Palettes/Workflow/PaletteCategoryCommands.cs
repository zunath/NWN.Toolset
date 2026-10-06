using Nwn.Authoring.Categories;

namespace Nwn.Toolset.Avalonia.Palettes.Workflow;

/// <summary>
/// The palette's category commands: new, rename, delete, pin and filing the selected entry. Each acts on
/// the controller's selected category, edits the live Custom section and then saves the sidecar.
/// </summary>
/// <remarks>
/// <para>
/// A refused save stops the command rather than reporting success over it: the save has already put the
/// reason in the status line, and the edit would otherwise exist only in memory and be gone on restart.
/// </para>
/// <para>
/// Folders are carried across a prompt or a save by path, never by object: the host may replace its tree
/// while a prompt is open (a reload) and does replace it when a refused save restores the persisted copy.
/// Each command re-reads the section and re-finds its folder after every await.
/// </para>
/// </remarks>
internal sealed class PaletteCategoryCommands
{
    private readonly PaletteWorkflowController _palette;

    public PaletteCategoryCommands(PaletteWorkflowController palette)
    {
        _palette = palette ?? throw new ArgumentNullException(nameof(palette));
    }

    private PaletteWorkflowTexts Texts => _palette.Texts;

    private PaletteWorkflowHost Host => _palette.Host;

    /// <summary>Adds a subcategory inside the selected one, or a top-level one when nothing is selected.</summary>
    public async Task NewCategoryAsync()
    {
        var type = _palette.SelectedType;
        var section = Host.Categories.Section(type);
        if (section == null || Host.Prompts is not { } prompts || !_palette.CanWrite)
            return;

        var parent = _palette.SelectedFolder;
        var parentPathKey = parent == null ? null : section.PathKey(parent);
        var name = await prompts.PromptForTextAsync(
            parent == null
                ? Texts.Get(PaletteWorkflowStringId.NewCategoryHeadline)
                : Texts.Get(PaletteWorkflowStringId.NewChildCategoryHeadline, parent.Name),
            Texts.Get(PaletteWorkflowStringId.NewCategoryMessage),
            string.Empty,
            Texts.Get(PaletteWorkflowStringId.CreateConfirm)).ConfigureAwait(true);

        if (name == null)
            return;

        // Checked rather than sanitized: the builder typed this and is still here to retype it. Asked
        // before the sibling check, so a name holding a separator is reported as that rather than as a
        // clash with whatever the split happened to land on.
        if (CategoryFolder.NameProblem(name) is { } problem)
        {
            _palette.StatusMessage = problem;
            return;
        }

        // Re-read after the prompt: the tree that was current when it opened may have been replaced.
        section = Host.Categories.Section(type);
        if (section == null)
            return;

        parent = parentPathKey == null ? null : section.FindByPathKey(parentPathKey);
        if (parentPathKey != null && parent == null)
            return;

        var nameAvailable = parent == null
            ? section.IsNameAvailable(name)
            : parent.IsNameAvailable(name);
        if (!nameAvailable)
        {
            _palette.StatusMessage = Texts.Get(PaletteWorkflowStringId.CategoryNameTaken, name.Trim());
            return;
        }

        if (parent != null)
            parent.AddChild(name);
        else
            section.AddFolder(name);

        if (!_palette.SaveCategories())
        {
            _palette.Refresh();
            return;
        }

        _palette.Refresh();
        _palette.StatusMessage = Texts.Get(PaletteWorkflowStringId.CategoryAdded, name);
        _palette.Log(Texts.Get(
            PaletteWorkflowStringId.CategoryAddedLog,
            name,
            Host.Content.PluralName(_palette.SelectedType).ToLowerInvariant()));
    }

    /// <summary>Renames the selected category, prompting with its current name.</summary>
    public async Task RenameCategoryAsync()
    {
        if (_palette.SelectedFolder is not { } folder || _palette.SelectedFolderPathKey is not { } pathKey ||
            Host.Prompts is not { } prompts)
        {
            return;
        }

        var name = await prompts.PromptForTextAsync(
            Texts.Get(PaletteWorkflowStringId.RenameCategoryHeadline, folder.Name),
            string.Empty,
            folder.Name,
            Texts.Get(PaletteWorkflowStringId.RenameConfirm)).ConfigureAwait(true);

        if (name == null || name == folder.Name)
            return;

        if (CategoryFolder.NameProblem(name) is { } problem)
        {
            _palette.StatusMessage = problem;
            return;
        }

        var previous = folder.Name;

        var section = _palette.CurrentSection();
        if (section?.FindByPathKey(pathKey) is not { } current)
            return;

        if (!section.TryRenameFolder(current, name))
        {
            _palette.StatusMessage = Texts.Get(PaletteWorkflowStringId.CategoryNameTaken, name.Trim());
            return;
        }

        var renamedPathKey = section.PathKey(current);
        var renamed = current.Name;
        if (!_palette.SaveCategories())
        {
            // The selection still names the old path, which the restored tree holds again.
            _palette.Refresh();
            return;
        }

        _palette.SelectedCategoryId = _palette.FolderCategoryId(renamedPathKey);
        _palette.Refresh();
        _palette.StatusMessage = Texts.Get(PaletteWorkflowStringId.CategoryRenamed, previous, renamed);
    }

    /// <summary>
    /// Deletes the selected category. Refuses one that still holds blueprints or sub-categories rather
    /// than confirming it: the contents would be silently unfiled with no way back.
    /// </summary>
    public async Task DeleteCategoryAsync()
    {
        var type = _palette.SelectedType;
        if (Host.Categories.Section(type) == null || _palette.SelectedFolder is not { } folder ||
            _palette.SelectedFolderPathKey is not { } pathKey || Host.Prompts is not { } prompts)
        {
            return;
        }

        if (folder.MembersIncludingDescendants.Any())
        {
            _palette.StatusMessage = Texts.Get(PaletteWorkflowStringId.CategoryHoldsBlueprints, folder.Name);
            return;
        }

        // Child categories count as contents too: an empty-but-organised branch would otherwise pass the
        // member check and go with its parent.
        if (folder.Children.Count > 0)
        {
            _palette.StatusMessage = Texts.Get(PaletteWorkflowStringId.CategoryHoldsSubcategories, folder.Name);
            return;
        }

        var confirmed = await prompts.ConfirmDestructiveAsync(
            Texts.Get(PaletteWorkflowStringId.DeleteCategoryHeadline, folder.Name),
            Texts.Get(PaletteWorkflowStringId.DeleteCategoryMessage),
            Texts.Get(PaletteWorkflowStringId.DeleteConfirm)).ConfigureAwait(true);

        if (!confirmed)
            return;

        // Re-found after the confirmation, and re-checked: the tree may have been replaced, or the folder
        // filled, while it was on screen.
        if (Host.Categories.Section(type) is not { } section || section.FindByPathKey(pathKey) is not { } current)
            return;

        if (current.MembersIncludingDescendants.Any())
        {
            _palette.StatusMessage = Texts.Get(PaletteWorkflowStringId.CategoryHoldsBlueprints, current.Name);
            return;
        }

        if (current.Children.Count > 0)
        {
            _palette.StatusMessage = Texts.Get(PaletteWorkflowStringId.CategoryHoldsSubcategories, current.Name);
            return;
        }

        section.RemoveFolder(current);
        _palette.SelectedCategoryId = null;
        if (!_palette.SaveCategories())
        {
            _palette.Refresh();
            return;
        }

        _palette.Refresh();
        _palette.StatusMessage = Texts.Get(PaletteWorkflowStringId.CategoryRemoved, folder.Name);
    }

    /// <summary>
    /// Pins or unpins the selected category. By path, not by name: two branches may hold folders of the
    /// same name, and pinning by name showed one while unpinning the other.
    /// </summary>
    public void TogglePin()
    {
        var section = Host.Categories.Section(_palette.SelectedType);
        if (section == null || _palette.SelectedFolder is not { } folder)
            return;

        var pathKey = section.PathKey(folder);
        if (section.Pinned.Contains(pathKey, StringComparer.OrdinalIgnoreCase))
            section.Unpin(pathKey);
        else
            section.Pin(pathKey);

        // Pinning leaves the path alone, so the selection already names this folder in whichever tree
        // the save leaves behind.
        _palette.SaveCategories();
        _palette.Refresh();
    }

    /// <summary>Moves the selected blueprint out of every category it was in and into the selected one.</summary>
    public void FileSelectedEntry()
    {
        if (!_palette.CanWrite)
            return;

        if (_palette.PresentationState.SelectedTile?.Snapshot is not { Kind: PaletteEntryKind.Blueprint } entry)
        {
            _palette.StatusMessage = Texts.Get(PaletteWorkflowStringId.SelectBlueprintFirst);
            _palette.PublishPresentationSnapshot();
            return;
        }

        if (_palette.SelectedFolder is not { } folder)
        {
            _palette.StatusMessage = Texts.Get(PaletteWorkflowStringId.SelectCategoryToFileInto);
            _palette.PublishPresentationSnapshot();
            return;
        }

        var section = Host.Categories.Section(_palette.SelectedType);
        if (section is null)
            return;

        var resRef = entry.ResRef;
        var label = entry.Name;
        foreach (var previous in section.FoldersContaining(resRef).ToList())
            previous.RemoveMember(resRef);

        folder.AddMember(resRef);
        if (!_palette.SaveCategories())
        {
            _palette.Refresh();
            return;
        }

        _palette.Refresh();
        _palette.StatusMessage = Texts.Get(PaletteWorkflowStringId.Filed, label, folder.Name);
        _palette.PublishPresentationSnapshot();
    }
}
