#nullable disable
using System.Collections.Generic;

namespace Nwn.Authoring.Areas.Generation
{
    public sealed class LayoutRoom
    {
        public int Id { get; set; }
        public RoomRole Role { get; set; }
        public (int X, int Y) CenterTile { get; set; }
        public List<(int X, int Y)> Tiles { get; set; } = new();
        public bool IsSetPiece { get; set; }
        public string OpenTerrain { get; set; } = string.Empty;
    }
}

