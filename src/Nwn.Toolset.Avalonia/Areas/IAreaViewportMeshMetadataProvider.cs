using Nwn.Preview.Scene;

namespace Nwn.Toolset.Avalonia.Areas;

/// <summary>Resolves host-specific metadata without adding host identifiers to shared render meshes.</summary>
public interface IAreaViewportMeshMetadataProvider
{
    long Revision { get; }

    AreaViewportMeshMetadata GetMetadata(RenderMesh mesh);
}
