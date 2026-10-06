using Nwn.Authoring.Resources;
namespace Nwn.Toolset.Avalonia.Areas.Contents;
/// <summary>A native instance-list section supplied by an area editor.</summary>
public sealed record AreaContentsSection(ModuleResourceType ResourceType, string Title, IReadOnlyList<AreaContentsEntry> Entries);


