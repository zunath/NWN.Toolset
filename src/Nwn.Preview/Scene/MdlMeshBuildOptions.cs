using Nwn.Formats.NativeModels;

namespace Nwn.Preview.Scene;

public sealed record MdlMeshBuildOptions
{
    public MdlMeshBuildPurpose Purpose { get; init; } = MdlMeshBuildPurpose.Render;
    public IReadOnlyList<IReadOnlyDictionary<string, PosedNode>>? PoseFrames { get; init; }
    public IReadOnlyList<MdlSampledAnimation>? Animations { get; init; }
    public float SkinSurfaceClearance { get; init; }
    public IReadOnlySet<string>? SkinSurfaceClearanceExcludedBones { get; init; }
    public Func<MdlTrimeshNode, bool>? IncludeRenderedMesh { get; init; }
    public Func<MdlTrimeshNode, bool>? IncludeEditorSelectionMesh { get; init; }
    public Func<MdlEmitterNode, bool>? IncludePersistentEmitter { get; init; }
    public Func<string, bool>? StateShowsEmitters { get; init; }
    public Func<MdlModel, MdlAnimation?>? ChooseDefaultPlaceableAnimation { get; init; }
    public Action<MdlTrimeshNode, RenderMesh>? MeshBuilt { get; init; }
}
