namespace Nwn.Toolset.Avalonia.Areas.Contents;
/// <summary>Immutable contents supplied by the currently active area editor.</summary>
public sealed record AreaContentsSnapshot(string AreaResRef, IReadOnlyList<AreaContentsSection> Sections);


