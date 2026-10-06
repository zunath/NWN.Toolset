namespace Nwn.Formats.Mdl;

/// <summary>One ASCII trimesh face with independent position and texture-coordinate indices.</summary>
public readonly record struct MdlTriangle(
    int VertexA,
    int VertexB,
    int VertexC,
    int SmoothingGroup,
    int TextureA,
    int TextureB,
    int TextureC,
    int MaterialIndex);
