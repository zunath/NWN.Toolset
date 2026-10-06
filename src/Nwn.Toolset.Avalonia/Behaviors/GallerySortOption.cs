namespace Nwn.Toolset.Avalonia.Behaviors
{
    public sealed record GallerySortOption(GallerySortMode Mode, string Display)
    {
        public override string ToString() => Display;
    }
}
