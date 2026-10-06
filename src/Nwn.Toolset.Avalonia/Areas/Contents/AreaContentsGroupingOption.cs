namespace Nwn.Toolset.Avalonia.Areas.Contents;
public sealed record AreaContentsGroupingOption(AreaContentsGrouping Value, string Label, string Description)
{
    public override string ToString() => Label;
}


