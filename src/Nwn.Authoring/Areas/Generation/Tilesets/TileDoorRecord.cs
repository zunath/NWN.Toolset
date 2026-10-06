#nullable disable

namespace Nwn.Authoring.Areas.Generation.Tilesets
{
    /// <summary>One door slot from a tileset tile record.</summary>
    public class TileDoorRecord
    {
        /// <summary>
        /// Zero for a generic doorway, otherwise the required tileset-specific doortypes.2da row.
        /// </summary>
        public int Type { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Orientation { get; set; }
    }
}

