using Nwn.Formats.Gff;

namespace Nwn.Authoring.Categories
{
    /// <summary>
    /// Turns a base-game palette resource into a <see cref="StandardPalette"/>. Which resource that is,
    /// and where it comes from, is the host's decision.
    /// </summary>
    public static class StandardPaletteReader
    {
        /// <summary>
        /// Imports a binary <c>.itp</c> and narrows its membership to the blueprints that really resolve.
        /// </summary>
        /// <param name="itpBytes">The palette resource as stored in the game's archives.</param>
        /// <param name="blueprintExists">
        /// Whether a resref names a blueprint the host can actually load. The palette is a manifest of what
        /// BioWare shipped across every expansion, so it names blueprints a given install does not have;
        /// a tile for one of those could never open or place.
        /// </param>
        /// <param name="resolveStrRef">Category and leaf names, which the base game stores as TLK references.</param>
        /// <param name="limits">Bounds on the tree, or null for <see cref="ItpImportLimits.Default"/>.</param>
        /// <exception cref="FormatException">The bytes are not a readable palette.</exception>
        public static StandardPalette Read(
            byte[] itpBytes,
            Func<string, bool> blueprintExists,
            Func<uint, string?>? resolveStrRef = null,
            ItpImportLimits? limits = null)
        {
            ArgumentNullException.ThrowIfNull(itpBytes);
            ArgumentNullException.ThrowIfNull(blueprintExists);

            var document = GffReader.Read(itpBytes);
            var section = ItpCategoryImporter.Import(document, out var names, resolveStrRef, limits);

            var resolvable = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var resRef in section.AssignedResRefs())
            {
                if (blueprintExists(resRef))
                    resolvable.Add(resRef);
            }

            return new StandardPalette(section, resolvable, names);
        }
    }
}
