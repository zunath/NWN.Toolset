using Nwn.Authoring.Documents.Native;

namespace Nwn.Authoring.Categories
{
    /// <summary>
    /// One palette import's recursive walk: the TLK resolver and how much of the node budget is left.
    /// </summary>
    internal sealed class ItpImportWalk
    {
        private readonly Func<uint, string?>? _resolveStrRef;
        private readonly ItpImportLimits _limits;
        private int _remaining;

        public ItpImportWalk(Func<uint, string?>? resolveStrRef, ItpImportLimits limits)
        {
            _resolveStrRef = resolveStrRef;
            _limits = limits ?? throw new ArgumentNullException(nameof(limits));
            _remaining = limits.MaximumNodes;
        }

        /// <summary>
        /// Converts a level of palette nodes into folders. Members found at this level belong to the
        /// caller's folder, so they are surfaced through <paramref name="membersForParent"/> rather than
        /// invented a folder for.
        /// </summary>
        public List<CategoryFolder> ImportChildren(
            IEnumerable<PaletteNode> nodes,
            int depth,
            List<string>? membersForParent,
            Dictionary<string, string>? names)
        {
            if (depth > _limits.MaximumDepth)
                throw new FormatException("The ITP category tree exceeds the maximum nesting depth.");

            var folders = new List<CategoryFolder>();

            foreach (var node in nodes)
            {
                if (--_remaining < 0)
                    throw new FormatException("The ITP category tree exceeds the maximum node count.");

                if (node.DeleteMe == true)
                    continue;

                if (!string.IsNullOrWhiteSpace(node.ResRef))
                {
                    var resRef = node.ResRef.Trim();
                    membersForParent?.Add(resRef);

                    if (names != null && ResolveName(node, out _) is { } leafName)
                        names[resRef] = leafName;

                    continue;
                }

                var children = node.Children;
                if (children.Count == 0)
                    continue;

                var name = ResolveName(node, out var isPlaceholder);
                if (name == null)
                {
                    // Transparent wrapper: hoist rather than create a folder nobody can name or find.
                    folders.AddRange(ImportChildren(children, depth + 1, membersForParent, names));
                    continue;
                }

                var folder = new CategoryFolder(name) { IsUnresolvedPlaceholder = isPlaceholder };
                var members = new List<string>();
                foreach (var child in ImportChildren(children, depth + 1, members, names))
                    folder.AddChild(child);

                foreach (var member in members)
                    folder.AddMember(member);

                // An empty branch carries no arrangement worth importing - the base-game palettes are
                // full of categories a module never filed anything into.
                if (folder.MembersIncludingDescendants.Any())
                    folders.Add(folder);
            }

            return folders;
        }

        /// <summary>
        /// A palette node's display name, or null when it has none to offer, and whether the returned
        /// name is the "Category N" placeholder invented because the resolver had nothing to offer.
        /// </summary>
        /// <remarks>
        /// Sanitised, because these names are the base game's and a folder name may not hold a path
        /// separator: the standard item palette ships "Skin/Hide" and "Crafting/Tradeskill Material".
        /// The placeholder marker is set here, the one place that fact is known for certain, rather than
        /// reconstructed later from the text.
        /// </remarks>
        private string? ResolveName(PaletteNode node, out bool isPlaceholder)
        {
            isPlaceholder = false;

            if (!string.IsNullOrWhiteSpace(node.Name))
                return CategoryFolder.Sanitize(node.Name);

            if (node.StrRef is not { } strRef)
                return null;

            var resolved = _resolveStrRef?.Invoke(strRef);
            var sanitized = string.IsNullOrWhiteSpace(resolved) ? null : CategoryFolder.Sanitize(resolved);
            if (sanitized != null)
                return sanitized;

            isPlaceholder = true;
            return CategoryPlaceholderNames.For(strRef);
        }
    }
}
