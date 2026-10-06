namespace Nwn.Formats.Tests.Mdl;

internal static class MdlFixtures
{
    public static string StaticMeshModel(bool withAnimation) =>
        "newmodel part\n" +
        "setsupermodel part NULL\n" +
        "beginmodelgeom part\n" +
        "node dummy root\n" +
        "  parent NULL\n" +
        "  position 1 0 0\n" +
        "endnode\n" +
        "node trimesh plate\n" +
        "  parent root\n" +
        "  position 0 2 0\n" +
        "  orientation 0 0 1 1.5707963\n" +
        "  scale 2\n" +
        "  bitmap armor_diffuse\n" +
        "  materialname armor_surface\n" +
        "  ambient 1 1 1\n" +
        "  verts 3\n" +
        "    1 0 0\n" +
        "    0 1 0\n" +
        "    0 0 1\n" +
        "  normals 3\n" +
        "    1 0 0\n" +
        "    1 0 0\n" +
        "    1 0 0\n" +
        "  tverts 3\n" +
        "    0 0 0\n" +
        "    1 0 0\n" +
        "    1 1 0\n" +
        "  faces 1\n" +
        "    0 1 2 7 0 1 2 3\n" +
        "endnode\n" +
        "endmodelgeom\n" +
        (withAnimation ? "newanim idle part\nnode dummy root\nendnode\ndoneanim idle part\n" : string.Empty);
}
