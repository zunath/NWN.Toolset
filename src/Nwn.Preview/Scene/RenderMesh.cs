// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Nwn.Preview.Scene
{
    public sealed class RenderMesh
    {
        /// <summary>Source MDL node name (for diagnostics/debugging, not guaranteed unique).</summary>
        public required string NodeName { get; init; }

        /// <summary>
        /// Lowercased primary texture (bitmap) resref for this mesh, or empty when the mesh has
        /// no bitmap assigned (e.g. a "NULL" bitmap in the source MDL).
        /// </summary>
        public required string TextureName { get; init; }

        /// <summary>
        /// Explicit NWN:EE material bound by the source model. Empty means <see cref="TextureName"/>
        /// is a bitmap and must not be replaced by an unrelated same-named MTR.
        /// </summary>
        public string MaterialName { get; init; } = string.Empty;

        /// <summary>Vertex positions in node-local space, 3 floats (x, y, z) per vertex.</summary>
        public required float[] Positions { get; init; }

        /// <summary>
        /// Vertex normals, 3 floats (x, y, z) per vertex, parallel to <see cref="Positions"/>.
        /// Empty when the source mesh had no normal array or a mismatched count.
        /// </summary>
        public required float[] Normals { get; init; }

        /// <summary>
        /// Primary UV set, 2 floats (u, v) per vertex, parallel to <see cref="Positions"/>.
        /// Empty when the source mesh had no UV set or a mismatched count.
        /// </summary>
        public required float[] TexCoords { get; init; }

        /// <summary>Triangle face indices into the vertex arrays above, 3 ints per face.</summary>
        public required int[] Indices { get; init; }

        /// <summary>
        /// The node's MDL diffuse colour, which multiplies the texture rather than replacing it.
        /// White for the great majority of meshes, and for any that do not state one.
        /// </summary>
        /// <remarks>
        /// Carried because for some models it is the only colour there is. Every waypoint marker in
        /// the haks - the cyan flag, the orange one, the treasure chest - is drawn on
        /// <c>tcn01_white</c> and coloured entirely by this, so a pipeline that samples the texture
        /// alone renders the whole set as identical white shapes.
        /// </remarks>
        public Vector3 DiffuseColor { get; init; } = Vector3.One;

        /// <summary>
        /// The source node's MDL <c>tilefade</c> flag: 0 for geometry that is always drawn, non-zero
        /// for geometry the engine fades out when the camera would otherwise be looking through it.
        /// </summary>
        /// <remarks>
        /// This is how a tileset marks what is overhead. Every <c>ceilling*</c> node of the zsf01
        /// interior tiles carries tilefade 1, as does the high <c>treefol_01</c> canopy shell of the
        /// ttw01 forest tiles - and nothing at floor or wall height does. Aurora's area view drops all
        /// of it, which is why a builder can see into rooms from above and see the forest floor at all;
        /// see <c>GlAreaControl.ShowCeilings</c>.
        /// </remarks>
        public int TileFade { get; init; }

        /// <summary>
        /// Accumulated node-to-model transform: this node's own SRT composed with every ancestor
        /// up to (but not including) a transform for the model root itself. See the MDL scene preparation transform composition.
        /// </summary>
        public required Matrix4x4 Transform { get; init; }

        /// <summary>
        /// This mesh's node-to-model transform at each frame of the idle, or empty when the model has
        /// no idle to play. <see cref="Transform"/> is the last of them - where the animation comes to
        /// rest - so anything that wants the settled model rather than the playback uses that.
        /// </summary>
        public IReadOnlyList<Matrix4x4> PoseFrames { get; init; } = Array.Empty<Matrix4x4>();

        /// <summary>
        /// Model-space vertex positions for each idle frame of a skinned mesh. Empty for rigid
        /// meshes. Each entry is parallel to <see cref="Positions"/>, whose values remain the final
        /// resting frame used by still thumbnails, bounds, and non-animated draws.
        /// </summary>
        public IReadOnlyList<float[]> PosePositions { get; init; } = Array.Empty<float[]>();

        /// <summary>
        /// Model-space vertex normals parallel to <see cref="PosePositions"/>. A frame may be empty
        /// when the source skinmesh has no complete normal array.
        /// </summary>
        public IReadOnlyList<float[]> PoseNormals { get; init; } = Array.Empty<float[]>();

        public IReadOnlyDictionary<string, IReadOnlyList<Matrix4x4>> AnimationFrames { get; init; } =
            new Dictionary<string, IReadOnlyList<Matrix4x4>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Model-space skinned positions for each frame of a named preview animation.</summary>
        public IReadOnlyDictionary<string, IReadOnlyList<float[]>> AnimationPositions { get; init; } =
            new Dictionary<string, IReadOnlyList<float[]>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Model-space skinned normals parallel to <see cref="AnimationPositions"/>.</summary>
        public IReadOnlyDictionary<string, IReadOnlyList<float[]>> AnimationNormals { get; init; } =
            new Dictionary<string, IReadOnlyList<float[]>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Item-specific PLT palette rows for this mesh. Usually empty, in which case the owning
        /// creature/model palette applies; weighted equipment such as a cloak carries its own dyes.
        /// </summary>
        public IReadOnlyDictionary<int, int> LayerColorIndices { get; set; } =
            new Dictionary<int, int>();

        /// <summary>
        /// The mesh is supplied by equipped creature armor, a helmet, or a cloak. Creature-blueprint
        /// material-dye tint locals are stored on that item. Semantic skin, hair and tattoo layers
        /// still come from the creature and are merged by creature preview renderers.
        /// </summary>
        public bool UsesItemTintOverrides { get; set; }

        /// <summary>
        /// Stored TM_* values from the equipped item that supplied this mesh. Empty for creature-
        /// owned and ordinary model geometry.
        /// </summary>
        public IReadOnlyDictionary<string, int> TintMapOverrides { get; set; } =
            new Dictionary<string, int>(StringComparer.Ordinal);

        public int VertexCount => Positions.Length / 3;
        public int TriangleCount => Indices.Length / 3;
    }

}


