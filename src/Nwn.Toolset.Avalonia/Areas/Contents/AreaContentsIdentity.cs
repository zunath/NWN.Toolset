using Nwn.Authoring.Resources;
namespace Nwn.Toolset.Avalonia.Areas.Contents;
/// <summary>Stable native list identity used to reconnect a row to its owning host.</summary>
public readonly record struct AreaContentsIdentity(ModuleResourceType ResourceType, int InstanceIndex);


