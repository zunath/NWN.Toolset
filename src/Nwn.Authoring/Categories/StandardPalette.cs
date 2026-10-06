namespace Nwn.Authoring.Categories
{
    /// <summary>
    /// The base game's own palette tree for one blueprint type: Aurora's "Standard" half of the palette,
    /// alongside the module's "Custom" content.
    /// </summary>
    /// <remarks>
    /// This describes what the game ships, not a decision a builder made, so it is deliberately not part
    /// of <see cref="CategoryCatalog"/> and cannot be written to the category sidecar.
    /// </remarks>
    public sealed class StandardPalette
    {
        /// <summary>What every failure and every unsupported type resolves to: no folders, no resrefs.</summary>
        public static StandardPalette Empty { get; } = new(
            new CategorySection(),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

        public StandardPalette(
            CategorySection section,
            IReadOnlySet<string> resRefs,
            IReadOnlyDictionary<string, string> names)
        {
            Section = section ?? throw new ArgumentNullException(nameof(section));
            ResRefs = resRefs ?? throw new ArgumentNullException(nameof(resRefs));
            Names = names ?? throw new ArgumentNullException(nameof(names));
        }

        /// <summary>
        /// Display names the palette file declares for its own entries, by resref. The base game's
        /// blueprints are not in the module, so the palette file is the only place one exists.
        /// </summary>
        public IReadOnlyDictionary<string, string> Names { get; }

        /// <summary>The imported category tree. Empty when the base game or the palette is unavailable.</summary>
        public CategorySection Section { get; }

        /// <summary>
        /// The resrefs this palette offers that actually resolve to a real resource. Verified against the
        /// host's resources rather than taken from the palette file, which lists blueprints an install may
        /// not have (expansion content, cut resources) and would otherwise show as dead tiles.
        /// </summary>
        public IReadOnlySet<string> ResRefs { get; }

        /// <summary>True when there is nothing to show, so a caller can skip the group entirely.</summary>
        public bool IsEmpty => ResRefs.Count == 0 && Section.Folders.Count == 0;
    }
}
