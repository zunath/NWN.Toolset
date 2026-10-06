using Nwn.Authoring.Documents.NimGff;

namespace Nwn.Authoring.Documents.Native
{
    /// <summary>
    /// One node of a palette tree: either a category (typically NAME or STRREF plus a nested
    /// "LIST" of children) or a leaf blueprint reference (typically RESREF, and for creatures
    /// also NAME/FACTION/CR). CC and DELETE_ME are exposed defensively (null when absent) because
    /// they are toolset-authored members that shipped palettes rarely carry.
    /// </summary>
    public sealed class PaletteNode
    {
        private readonly JsonGffStruct _struct;

        internal PaletteNode(JsonGffStruct target)
        {
            _struct = target;
        }

        /// <summary>The underlying struct, for members this view does not name.</summary>
        public JsonGffStruct Struct => _struct;

        public int? Id => _struct.GetIntOrNull("ID");

        public uint? StrRef => _struct.GetUIntOrNull("STRREF");

        public string? Name => _struct.GetStringOrNull("NAME");

        public string? ResRef => _struct.GetStringOrNull("RESREF");

        public string? Faction => _struct.GetStringOrNull("FACTION");

        public float? ChallengeRating => _struct.GetSingleOrNull("CR");

        /// <summary>Rare in shipped palettes; present for compatibility with toolset-authored files.</summary>
        public string? Cc => _struct.GetStringOrNull("CC");

        /// <summary>Rare in shipped palettes; present for compatibility with toolset-authored files.</summary>
        public bool? DeleteMe => _struct.GetIntOrNull("DELETE_ME") is { } value ? value != 0 : null;

        /// <summary>This node's children ("LIST"), empty for leaf nodes.</summary>
        public IReadOnlyList<PaletteNode> Children =>
            _struct.GetListOrEmpty("LIST").Select(s => new PaletteNode(s)).ToList();
    }
}
