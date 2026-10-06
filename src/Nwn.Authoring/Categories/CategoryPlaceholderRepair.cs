namespace Nwn.Authoring.Categories
{
    /// <summary>
    /// Renames categories that were imported before their TLK was available.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Category names are resolved once, at import, and then persisted - so a module first opened
    /// without the base game's dialog.tlk has "Category 6782" written into its sidecar permanently, and
    /// simply supplying the TLK later fixes nothing. Repairing on load is preferable to bumping the
    /// sidecar version and re-seeding, which would also discard every category a builder made.
    /// </para>
    /// <para>
    /// Provenance comes from <see cref="CategoryFolder.IsUnresolvedPlaceholder"/>, set only by
    /// <see cref="ItpCategoryImporter"/> at the moment it invents the placeholder text, never inferred
    /// from the name. A builder can deliberately name a folder "Category 7"; matching on text alone used
    /// to rename (and save over) exactly that deliberate name. A sidecar written before the marker
    /// existed therefore keeps its placeholders until a builder renames them - a deliberate name
    /// surviving is worth more than auto-repairing every legacy placeholder.
    /// </para>
    /// </remarks>
    public static class CategoryPlaceholderRepair
    {
        /// <summary>
        /// Renames every marked placeholder the resolver can now name, and returns how many changed. The
        /// caller saves the section when the count is non-zero.
        /// </summary>
        public static int Repair(CategorySection section, Func<uint, string?> resolveStrRef)
        {
            ArgumentNullException.ThrowIfNull(section);
            ArgumentNullException.ThrowIfNull(resolveStrRef);

            var repaired = 0;
            foreach (var folder in section.AllFolders().ToList())
            {
                if (!folder.IsUnresolvedPlaceholder)
                    continue;

                if (!CategoryPlaceholderNames.TryParse(folder.Name, out var strRef))
                    continue;

                // Sanitized like every other name that comes out of the TLK: several of the base game's
                // category names carry a path separator, and this repair runs over a tree that is already
                // loaded and on screen, so a throw here would take the open module with it.
                var resolved = CategoryFolder.Sanitize(resolveStrRef(strRef));
                if (resolved == null)
                    continue;

                // A pin is stored by path, and a path is built from names, so the rename goes through the
                // section, which moves the pins with it. Renaming also clears IsUnresolvedPlaceholder.
                if (section.TryRenameFolder(folder, resolved))
                    repaired++;
            }

            return repaired;
        }
    }
}
