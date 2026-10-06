using Nwn.Preview.Areas;
using Nwn.Preview.Scene;

namespace Nwn.Toolset.Avalonia.Areas;

/// <summary>Typed input for resolving a host resource and its per-instance appearance.</summary>
public sealed record AreaViewportMaterialRequest(
    string? TextureName,
    string? MaterialName,
    RenderMesh? Mesh,
    RenderModel? Model,
    InstanceMarker? Instance,
    AreaViewportMeshMetadata? MeshMetadata,
    AreaViewportDrawPurpose Purpose);
