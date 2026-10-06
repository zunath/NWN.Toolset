#nullable disable
namespace Nwn.Authoring.Areas.Generation
{
    public sealed class TunnelLink
    {
        public (int X, int Y) CornerA { get; set; }
        public (int X, int Y) CornerB { get; set; }
        public int Length { get; set; }
    }
}

