#nullable disable
using System.Collections.Generic;

namespace Nwn.Authoring.Areas.Generation.Tilesets
{
    /// <summary>One pre-designed multi-tile group.</summary>
    public class TileGroupRecord
    {
        public string Name { get; set; } = string.Empty;
        public int Rows { get; set; }
        public int Columns { get; set; }
        public List<int> TileIds { get; set; } = new();
    }
}

