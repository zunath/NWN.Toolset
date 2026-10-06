#nullable disable
using Nwn.Authoring.Areas.Generation.Tilesets;

namespace Nwn.Authoring.Areas.Generation.Drafting
{
    /// <summary>Static numeric ranges for the authoring surface.</summary>
    public static class AreaSettingsBounds
    {
        public const int WidthMin = 8;
        public const int WidthMax = 32;
        public const int HeightMin = 8;
        public const int HeightMax = 32;
        public const int MinRoomsMin = 2;
        public const int MinRoomsMax = 12;
        public const int MaxRoomsMin = 2;
        public const int MaxRoomsMax = 16;
        public const int MinRoomSizeMin = 2;
        public const int MinRoomSizeMax = 10;
        public const int MaxRoomSizeMin = 3;
        public const int MaxRoomSizeMax = 12;
        public const int CorridorWidthMin = 1;
        public const int CorridorWidthMax = 3;
        public const int LoopFactorPercentMin = 0;
        public const int LoopFactorPercentMax = 100;
        public const int OrganicFillPercentMin = 30;
        public const int OrganicFillPercentMax = 60;
        public const int AccentDensityPercentMin = 1;
        public const int AccentDensityPercentMax = 20;
        public const int FeatureDensityPercentMin = 0;
        public const int FeatureDensityPercentMax = 15;
        public const int ElevationRegionsMin = 0;
        public const int ElevationRegionsMax = 3;
        public const int EntrancesMin = 1;
        public const int EntrancesMax = 3;
        public const int ExitsMin = 1;
        public const int ExitsMax = 3;
        public const int DecorationDensityPercentMin = 0;
        public const int DecorationDensityPercentMax = 200;
        public const int RoomSizeSliderAbsoluteMax = 12;
        public const int MaxSeed = int.MaxValue - 1000;
    }
}
