using Nwn.Authoring.Categories;
using Nwn.Authoring.Resources;
using Nwn.Formats.Resources;

namespace Nwn.Toolset.Avalonia.Palettes.Workflow;

/// <summary>
/// The palette's entry commands: place, edit, Edit Copy, delete and create. Placing and editing are the
/// only things a builder does with a blueprint, so they are the tile's two verbs; the rest live on menus.
/// </summary>
/// <remarks>
/// Every command first checks that the entry it was handed still belongs to the snapshot on screen, so a
/// menu opened before the type or source changed cannot act on the old one.
/// </remarks>
internal sealed class PaletteEntryCommands
{
    private readonly PaletteWorkflowController _palette;

    public PaletteEntryCommands(PaletteWorkflowController palette)
    {
        _palette = palette ?? throw new ArgumentNullException(nameof(palette));
    }

    private PaletteWorkflowTexts Texts => _palette.Texts;

    private PaletteWorkflowHost Host => _palette.Host;

    public void Place(PaletteEntrySnapshot entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (!_palette.TryGetCurrentEntry(entry, out var current) || !current.Capabilities.CanPlace)
            return;

        var target = Host.Placement?.ActiveTarget;
        if (target is null)
        {
            _palette.StatusMessage = _palette.PaletteTexts.OpenAreaToPlace;
            _palette.PublishPresentationSnapshot();
            return;
        }

        if (current.Kind == PaletteEntryKind.Tile)
        {
            if (_palette.TryGetTile(current.Id, out var tile))
            {
                _palette.StatusMessage = target.ArmTilePlacement(tile)
                    ? Texts.Get(PaletteWorkflowStringId.ArmTile, tile.Label)
                    : Texts.Get(PaletteWorkflowStringId.NoTileGrid);
            }
        }
        else if (target.ArmPlacement(_palette.SelectedType, current.ResRef, current.Source))
        {
            _palette.StatusMessage = Texts.Get(PaletteWorkflowStringId.ArmBlueprint, current.Name);
        }
        else
        {
            _palette.StatusMessage = Texts.Get(
                PaletteWorkflowStringId.TypeCannotBePlaced,
                Host.Content.PluralName(_palette.SelectedType));
        }

        _palette.PublishPresentationSnapshot();
    }

    public void Edit(PaletteEntrySnapshot entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (_palette.TryGetCurrentEntry(entry, out var current) &&
            current.Kind == PaletteEntryKind.Blueprint &&
            current.Capabilities.CanEdit)
        {
            Host.Blueprints?.OpenEditor(_palette.SelectedType, current.ResRef);
        }
    }

    /// <summary>
    /// Creates an independent Custom blueprint from the selected one, files it under the matching Custom
    /// category, reveals it and opens it for editing. The source and its placed instances are untouched.
    /// A host that makes the copy in an editor of its own reports that instead, and nothing is filed.
    /// </summary>
    public async Task EditCopyAsync(PaletteEntrySnapshot entry, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);
        cancellationToken.ThrowIfCancellationRequested();
        if (!_palette.TryGetCurrentEntry(entry, out var current) ||
            current.Kind != PaletteEntryKind.Blueprint ||
            !current.Capabilities.CanEditCopy ||
            !_palette.CanEditCopy)
        {
            return;
        }

        if (!Host.Content.IsModuleOpen || Host.Blueprints is not { } blueprints)
            return;

        var type = _palette.SelectedType;
        var typeKey = _palette.TypeKey;
        var isStandard = current.Source == PaletteSource.Standard;
        var sourceSection = isStandard
            ? Host.Content.Standard(type).Section
            : Host.Categories.Section(type);

        // A parent row lists all its descendants, so prefer the leaf below the row whose menu was used;
        // search has no category context, so its stable first filing is the best answer there.
        var selectedFolder = _palette.PresentationState.IsSearching ? null : _palette.SelectedFolder;
        var sourceFolder = PaletteCategoryPaths.SourceFolder(sourceSection, current.ResRef, selectedFolder);
        var sourcePath = sourceFolder == null || sourceSection == null
            ? Array.Empty<string>()
            : sourceSection.PathTo(sourceFolder).ToArray();

        var copy = await blueprints.CopyAsync(type, current.Source, current.ResRef, cancellationToken)
            .ConfigureAwait(true);
        switch (copy.Outcome)
        {
            case PaletteBlueprintCopyOutcome.OpenedInEditor:
                // The editor makes the copy and chooses its identity; there is nothing here to file yet.
                _palette.StatusMessage = Texts.Get(PaletteWorkflowStringId.CopyOpenedInEditor, current.Name);
                _palette.Log(Texts.Get(PaletteWorkflowStringId.CopyOpenedInEditorLog, typeKey, current.ResRef));
                return;
            case PaletteBlueprintCopyOutcome.Failed:
                _palette.StatusMessage = Texts.Get(PaletteWorkflowStringId.CopyFailed, current.Name, copy.Problem);
                _palette.Log(Texts.Get(PaletteWorkflowStringId.CopyFailedLog, typeKey, current.ResRef, copy.Problem));
                return;
        }

        var copyResRef = copy.ResRef!;
        string? targetPathKey = null;
        var filed = true;
        if (sourcePath.Length > 0 && Host.Categories.Section(type) is { } customSection)
        {
            var targetFolder = PaletteCategoryPaths.EnsureFolderPath(customSection, sourcePath);
            targetFolder.AddMember(copyResRef);
            filed = _palette.SaveCategories();
            if (filed)
                targetPathKey = customSection.PathKey(targetFolder);
        }

        // Edit Copy always lands on the Custom side. Reveal the new entry there before opening its
        // editor, matching Aurora and making the new blueprint immediately available for placement -
        // unless the builder moved to another type or to Tiles while a slow copy was being written.
        if (_palette.IsBlueprintMode && _palette.SelectedType == type)
        {
            if (!_palette.IsCustomSource)
                _palette.Source = PaletteSource.Custom;
            else
                _palette.Refresh();

            RevealCustomCopy(copyResRef, filed ? targetPathKey : null);
        }

        if (filed)
        {
            _palette.StatusMessage = Texts.Get(PaletteWorkflowStringId.Copied, current.Name, copyResRef);
            _palette.Log(Texts.Get(
                PaletteWorkflowStringId.CopiedLog, typeKey, current.ResRef, copyResRef, copy.Location));
        }
        else
        {
            var category = sourcePath.Length == 0
                ? Texts.Get(PaletteWorkflowStringId.CopySourceCategory)
                : sourcePath[^1];
            _palette.StatusMessage = Texts.Get(
                PaletteWorkflowStringId.CopiedUnfiled, current.Name, copyResRef, category, _palette.StatusMessage);
            _palette.Log(Texts.Get(
                PaletteWorkflowStringId.CopiedUnfiledLog, typeKey, current.ResRef, copyResRef, category));
        }

        blueprints.OpenEditor(type, copyResRef);
    }

    public Task DeleteAsync(PaletteEntrySnapshot entry, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);
        cancellationToken.ThrowIfCancellationRequested();
        if (!_palette.TryGetCurrentEntry(entry, out var current) ||
            current.Kind != PaletteEntryKind.Blueprint ||
            !current.Capabilities.CanDelete ||
            !_palette.CanWrite)
        {
            return Task.CompletedTask;
        }

        if (!Host.Content.IsModuleOpen || Host.Blueprints is not { } blueprints || Host.Prompts is not { } prompts)
            return Task.CompletedTask;

        return DeleteEntryAsync(current, _palette.SelectedType, _palette.Source, blueprints, prompts);
    }

    /// <summary>
    /// Creates a blueprint of the active type and files it into the selected category. It opens straight
    /// away: nobody creates a blueprint in order to leave it alone. The host's own creation UI runs instead
    /// of the name prompt for the types it handles.
    /// </summary>
    public async Task NewBlueprintAsync(CancellationToken cancellationToken)
    {
        if (!Host.Content.IsModuleOpen || Host.Blueprints is not { } blueprints || !_palette.CanCreateBlueprint)
            return;

        var type = _palette.SelectedType;
        var typeKey = _palette.TypeKey;
        var kind = Host.Content.SingularName(type);

        // The category is remembered by path: the tree may be replaced while a dialog or a write is open.
        var folderPathKey = _palette.SelectedFolderPathKey;

        string? resRef;
        string? name;
        PaletteBlueprintCreation creation;
        if (Host.CreationDialog is { } dialog && dialog.Handles(type))
        {
            creation = await dialog.CreateAsync(type, cancellationToken).ConfigureAwait(true);
            resRef = creation.ResRef;
            name = creation.Name ?? creation.ResRef;
            if (creation.Outcome == PaletteBlueprintCreationOutcome.Created && string.IsNullOrWhiteSpace(resRef))
            {
                throw new InvalidOperationException(
                    "A host creation dialog must name the blueprint it created (PaletteBlueprintCreation.CreatedByHost).");
            }
        }
        else
        {
            if (Host.Prompts is not { } prompts)
                return;

            name = await prompts.PromptForTextAsync(
                Texts.Get(PaletteWorkflowStringId.NewBlueprintHeadline, kind),
                Texts.Get(PaletteWorkflowStringId.NewBlueprintMessage, ResourceReferenceRules.MaxLength),
                string.Empty,
                Texts.Get(PaletteWorkflowStringId.CreateConfirm)).ConfigureAwait(true);

            if (name == null)
                return;

            resRef = PaletteResRefNames.FromDisplayName(name);
            if (resRef.Length == 0)
            {
                _palette.StatusMessage = Texts.Get(PaletteWorkflowStringId.NameHasNoResRefCharacters);
                return;
            }

            creation = await blueprints.CreateAsync(type, resRef, name, cancellationToken).ConfigureAwait(true);
        }

        switch (creation.Outcome)
        {
            case PaletteBlueprintCreationOutcome.Cancelled:
                return;
            case PaletteBlueprintCreationOutcome.AlreadyExists:
                _palette.StatusMessage = Texts.Get(
                    PaletteWorkflowStringId.BlueprintAlreadyExists, kind.ToLowerInvariant(), resRef ?? string.Empty);
                return;
            case PaletteBlueprintCreationOutcome.Failed:
                _palette.StatusMessage = Texts.Get(
                    PaletteWorkflowStringId.CreateFailed, resRef ?? kind, creation.Problem);
                _palette.Log(Texts.Get(
                    PaletteWorkflowStringId.CreateFailedLog, typeKey, resRef ?? string.Empty, creation.Problem));
                return;
        }

        var createdResRef = resRef!;
        var createdName = name ?? createdResRef;

        // Filed where the builder asked for it, which is the whole reason this lives on the category's
        // menu rather than a global New button.
        var filed = true;
        if (folderPathKey is not null && Host.Categories.Section(type)?.FindByPathKey(folderPathKey) is { } folder)
        {
            folder.AddMember(createdResRef);
            filed = _palette.SaveCategories();

            // The refused save restored the persisted catalog, so the blueprint exists but is in
            // Unsorted. Said rather than overwritten: only half of create-and-file happened.
            if (!filed)
            {
                _palette.StatusMessage = Texts.Get(
                    PaletteWorkflowStringId.CreatedUnfiled, createdName, folder.Name, _palette.StatusMessage);
                _palette.Log(Texts.Get(
                    PaletteWorkflowStringId.CreatedUnfiledLog, typeKey, createdResRef, folder.Name));
            }
        }

        _palette.Refresh();
        if (filed)
        {
            _palette.StatusMessage = Texts.Get(PaletteWorkflowStringId.Created, createdName);
            _palette.Log(Texts.Get(PaletteWorkflowStringId.CreatedLog, typeKey, createdResRef, creation.Location));
        }

        if (!creation.OpenedInEditor)
            blueprints.OpenEditor(type, createdResRef);
    }

    /// <summary>
    /// Deletes the blueprint. The one palette action that destroys something outside the sidecar: areas
    /// that placed it keep their instances, and those will no longer resolve.
    /// </summary>
    private async Task DeleteEntryAsync(
        PaletteEntrySnapshot entry,
        ModuleResourceType resourceType,
        PaletteSource source,
        IPaletteBlueprintOperations blueprints,
        IPalettePrompts prompts)
    {
        var kind = Host.Content.SingularName(resourceType).ToLowerInvariant();

        // Refused rather than handled: an open editor holds a session on this blueprint, and its next save
        // would either recreate it or fail. Closing it first is the builder's call.
        if (blueprints.IsOpenInEditor(resourceType, entry.ResRef))
        {
            _palette.StatusMessage = Texts.Get(PaletteWorkflowStringId.DeleteOpenInEditor, entry.Name);
            return;
        }

        // Asked before the delete: removing the blueprint is irreversible, and a sidecar that cannot be
        // written would leave its category pointing at nothing.
        if (!SidecarAcceptsUnfiling(entry, resourceType))
            return;

        var preparation = blueprints.PrepareDelete(resourceType, entry.ResRef);
        if (preparation.Deletion is not { } deletion)
        {
            _palette.StatusMessage = Texts.Get(PaletteWorkflowStringId.DeleteRefused, entry.Name, preparation.Problem);
            _palette.Log(Texts.Get(PaletteWorkflowStringId.DeleteRefusedLog, entry.ResRef, preparation.LogDetail));
            return;
        }

        using var heldDeletion = deletion;
        var confirmed = await prompts.ConfirmDestructiveAsync(
            Texts.Get(PaletteWorkflowStringId.DeleteBlueprintHeadline, kind, entry.Name),
            Texts.Get(PaletteWorkflowStringId.DeleteBlueprintMessage, deletion.DisplayName),
            Texts.Get(PaletteWorkflowStringId.DeleteConfirm)).ConfigureAwait(true);

        if (!confirmed)
            return;

        if (!deletion.IsCurrent ||
            _palette.SelectedType != resourceType || _palette.Source != source || _palette.IsTileMode)
        {
            return;
        }

        // Rechecked here, not just at the gate that greys the menu item: a pack, validation or build can
        // start while the confirmation is on screen.
        if (Host.WriteGate?.IsLocked == true)
        {
            _palette.StatusMessage = Texts.Get(PaletteWorkflowStringId.DeleteWhileLocked, entry.Name);
            _palette.Log(Texts.Get(PaletteWorkflowStringId.DeleteWhileLockedLog, entry.ResRef));
            return;
        }

        // The sidecar can change externally while the confirmation sits open; finding out only at the
        // final save would be too late, with the blueprint already gone.
        if (!SidecarAcceptsUnfiling(entry, resourceType))
            return;

        var commit = deletion.Commit();
        if (!commit.Succeeded)
        {
            _palette.StatusMessage = Texts.Get(PaletteWorkflowStringId.DeleteRefused, entry.Name, commit.Problem);
            _palette.Log(Texts.Get(PaletteWorkflowStringId.DeleteFailedLog, entry.ResRef, commit.Problem));
            return;
        }

        // Drop it from the sidecar too, or the category keeps a member that resolves to nothing.
        var unfiled = true;
        if (Host.Categories.Section(resourceType) is { } section)
        {
            foreach (var folder in section.FoldersContaining(entry.ResRef).ToList())
                folder.RemoveMember(entry.ResRef);

            unfiled = _palette.SaveCategories();
            if (!unfiled)
            {
                _palette.StatusMessage = Texts.Get(
                    PaletteWorkflowStringId.DeletedStillFiled, entry.Name, _palette.StatusMessage);
                _palette.Log(Texts.Get(PaletteWorkflowStringId.DeletedStillFiledLog, entry.ResRef));
            }
        }

        _palette.Refresh();
        if (unfiled)
        {
            _palette.StatusMessage = Texts.Get(PaletteWorkflowStringId.Deleted, entry.Name);
            _palette.Log(Texts.Get(PaletteWorkflowStringId.DeletedLog, entry.ResRef, deletion.Location));
        }
    }

    /// <summary>Whether the sidecar would accept dropping a filed blueprint, reporting a refusal.</summary>
    private bool SidecarAcceptsUnfiling(PaletteEntrySnapshot entry, ModuleResourceType resourceType)
    {
        if (Host.Categories.Section(resourceType)?.FoldersContaining(entry.ResRef).Any() != true)
            return true;

        var check = Host.Categories.CanSaveChanges();
        if (check.Saved)
            return true;

        _palette.StatusMessage = Texts.Get(PaletteWorkflowStringId.DeleteRefused, entry.Name, check.Problem);
        _palette.Log(Texts.Get(PaletteWorkflowStringId.DeleteRefusedLog, entry.ResRef, check.Problem));
        return false;
    }

    private void RevealCustomCopy(string copyResRef, string? targetPathKey)
    {
        var section = Host.Categories.Section(_palette.SelectedType);
        _palette.SelectedCategoryId = targetPathKey != null && section?.FindByPathKey(targetPathKey) is { } folder
            ? _palette.FolderCategoryId(section, folder)
            : _palette.UnsortedCategoryId();
        _palette.Refresh();

        var presentation = _palette.PresentationState;
        presentation.SelectedRow = presentation.Rows.FirstOrDefault(row => row.Id == _palette.SelectedCategoryId);
        var copyEntryId = _palette.BlueprintEntryId(copyResRef);
        presentation.SelectedTile = presentation.Tiles.FirstOrDefault(tile => tile.Id == copyEntryId);
    }
}
