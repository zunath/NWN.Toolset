#nullable disable
namespace Nwn.Authoring.Areas.Generation
{
    public static class LayoutStyleSizeFloor
    {
        public static int For(DungeonLayoutStyle style) => style switch
        {
            DungeonLayoutStyle.OrganicCave => 12,
            DungeonLayoutStyle.Warren => 8,
            DungeonLayoutStyle.PackedRooms => 9,
            DungeonLayoutStyle.RoomsAndCorridors => 11,
            DungeonLayoutStyle.Labyrinth => 8,
            _ => 12
        };
    }
}

