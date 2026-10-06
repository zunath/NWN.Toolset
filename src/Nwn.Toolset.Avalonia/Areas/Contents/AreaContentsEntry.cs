using System.Numerics;
using Nwn.Authoring.Resources;
namespace Nwn.Toolset.Avalonia.Areas.Contents;
/// <summary>A host-neutral view of one placed native resource.</summary>
public sealed record AreaContentsEntry(ModuleResourceType ResourceType, int InstanceIndex, string Name, string TemplateResRef, string Tag, Vector3 Position)
{
    public AreaContentsIdentity Identity => new(ResourceType, InstanceIndex);
}



