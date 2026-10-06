using System.Globalization;
using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Documents.NimGff;
using NativeGffDocument = Nwn.Formats.Gff.GffDocument;

namespace Nwn.Authoring.Categories
{
    /// <summary>
    /// Seeds a <see cref="CategorySection"/> from an NWN palette (<c>.itp</c>) tree, so a builder who
    /// already has categories does not start from an empty one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Import only.</b> The toolset reads <c>.itp</c> files and never writes them, because the game and
    /// the Aurora toolset rewrite them - anything saved there is eventually lost. Run this once to
    /// populate the sidecar, then the sidecar owns the arrangement.
    /// </para>
    /// <para>
    /// Three shapes appear in the real files and each needs different handling. A node carrying
    /// <c>RESREF</c> is a blueprint, and becomes a member of the folder enclosing it. A node carrying
    /// <c>NAME</c> or <c>STRREF</c> plus a child list is a category. A node with a child list but neither
    /// name is a transparent wrapper - the corpus puts one at the root of every palette - so its children
    /// are hoisted into its parent rather than filed under a nameless folder.
    /// </para>
    /// </remarks>
    public static class ItpCategoryImporter
    {
        /// <summary>The GFF file type every palette carries.</summary>
        public const string PaletteFileType = "ITP ";

        /// <summary>
        /// Builds a section from a palette tree. <paramref name="resolveStrRef"/> supplies category names
        /// for the base-game palettes, which label categories by TLK reference rather than by string;
        /// without it those folders fall back to a legible placeholder that a builder can rename.
        /// </summary>
        public static CategorySection Import(ItpDocument document, Func<uint, string?>? resolveStrRef = null) =>
            Import(document, out _, resolveStrRef);

        /// <summary>
        /// Builds a section and also hands back the display name each leaf declares.
        /// </summary>
        /// <remarks>
        /// A palette leaf carries a NAME or a STRREF of its own, and those are the only names the base
        /// game has for its blueprints - there is no module file to read one from. Discarding them is why
        /// the Standard palette showed a cryptic resref for nearly every entry.
        /// </remarks>
        /// <exception cref="FormatException">The tree exceeds <paramref name="limits"/>.</exception>
        public static CategorySection Import(
            ItpDocument document,
            out IReadOnlyDictionary<string, string> leafNames,
            Func<uint, string?>? resolveStrRef = null,
            ItpImportLimits? limits = null)
        {
            ArgumentNullException.ThrowIfNull(document);

            var walk = new ItpImportWalk(resolveStrRef, limits ?? ItpImportLimits.Default);
            var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var section = new CategorySection();
            foreach (var folder in walk.ImportChildren(document.Nodes, 0, membersForParent: null, names))
                section.AddFolder(folder);

            leafNames = names;
            return section;
        }

        /// <summary>
        /// Builds a section from a palette read by the native binary GFF reader, for hosts that load
        /// <c>.itp</c> resources from archives rather than from nwn_gff JSON.
        /// </summary>
        /// <exception cref="FormatException">The document is not a palette, or exceeds <paramref name="limits"/>.</exception>
        public static CategorySection Import(
            NativeGffDocument document,
            out IReadOnlyDictionary<string, string> leafNames,
            Func<uint, string?>? resolveStrRef = null,
            ItpImportLimits? limits = null)
        {
            ArgumentNullException.ThrowIfNull(document);
            if (!string.Equals(document.FileType, PaletteFileType, StringComparison.Ordinal))
            {
                throw new FormatException(string.Format(
                    CultureInfo.InvariantCulture,
                    "Expected an ITP GFF document, found '{0}'.",
                    document.FileType));
            }

            return Import(
                new ItpDocument(NativeGffBridge.ToJsonDocument(document)),
                out leafNames,
                resolveStrRef,
                limits);
        }
    }
}
