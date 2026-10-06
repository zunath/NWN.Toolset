using Nwn.Authoring.Documents.NimGff;

namespace Nwn.Authoring.Documents.Native
{
    /// <summary>
    /// Typed view over an .itp (palette tree) nwn_gff JSON document: a recursive "MAIN" list of
    /// category/leaf nodes.
    /// </summary>
    public sealed class ItpDocument : GffDocumentBase
    {
        public ItpDocument(JsonGffDocument document) : base(document)
        {
        }

        public static ItpDocument Load(string path) => new(JsonGffDocument.Load(path));

        public static ItpDocument Parse(byte[] content) => new(JsonGffDocument.Parse(content));

        /// <summary>The palette's top-level category nodes ("MAIN").</summary>
        public IReadOnlyList<PaletteNode> Nodes =>
            Root.GetListOrEmpty("MAIN").Select(s => new PaletteNode(s)).ToList();

        /// <summary>Whether any leaf in the recursive palette tree references this blueprint.</summary>
        public bool ContainsResRef(string resRef) =>
            Nodes.Any(node => ContainsResRef(node, resRef));

        private static bool ContainsResRef(PaletteNode node, string resRef) =>
            string.Equals(node.ResRef, resRef, StringComparison.OrdinalIgnoreCase) ||
            node.Children.Any(child => ContainsResRef(child, resRef));
    }
}
