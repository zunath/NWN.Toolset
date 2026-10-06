#nullable disable
namespace Nwn.Authoring.Areas.Generation
{
    public sealed class TransitionPoint
    {
        public TransitionKind Kind { get; set; }
        public (int X, int Y) Tile { get; set; }
        public int RoomId { get; set; }
        public TransitionStyle Style { get; set; } = TransitionStyle.Placeable;
        public (int X, int Y) DoorCell { get; set; }
        public (int X, int Y) DoorwayCell { get; set; }
        public float DoorX { get; set; }
        public float DoorY { get; set; }
        public float DoorZ { get; set; }
        public float DoorOrientation { get; set; }
        public int DoorType { get; set; }
    }
}

