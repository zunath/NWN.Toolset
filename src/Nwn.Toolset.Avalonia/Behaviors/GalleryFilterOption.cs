namespace Nwn.Toolset.Avalonia.Behaviors
{
    public sealed record GalleryFilterOption(string? ValueKey, string Display)
    {
        public override string ToString() => Display;
    }
}
