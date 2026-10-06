#nullable disable

namespace Nwn.Authoring.Areas.Generation.Decoration
{
    /// <summary>A weighted, multi-placeable decoration grouping.</summary>
    public class DungeonVignette
    {
        public string Key { get; set; } = string.Empty;
        public int Weight { get; set; } = 1;
        public List<DungeonVignetteMember> Members { get; set; } = new();
    }
}
