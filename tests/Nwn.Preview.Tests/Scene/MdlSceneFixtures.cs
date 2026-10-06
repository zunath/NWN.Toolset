namespace Nwn.Preview.Tests.Scene;

internal static class MdlSceneFixtures
{
    public static string StaticMeshModel(bool withAnimation) =>
        "newmodel part\n" +
        "beginmodelgeom part\n" +
        "node dummy root\nparent NULL\nposition 1 0 0\nendnode\n" +
        "node trimesh plate\nparent root\nposition 0 2 0\norientation 0 0 1 1.5707963\nscale 2\n" +
        "bitmap armor_diffuse\nmaterialname armor_surface\nambient 1 1 1\n" +
        "verts 3\n1 0 0\n0 1 0\n0 0 1\n" +
        "normals 3\n1 0 0\n1 0 0\n1 0 0\n" +
        "tverts 3\n0 0 0\n1 0 0\n1 1 0\n" +
        "faces 1\n0 1 2 7 0 1 2 3\nendnode\nendmodelgeom\n" +
        (withAnimation ? "newanim idle part\nnode dummy root\nendnode\ndoneanim idle part\n" : string.Empty);
}
