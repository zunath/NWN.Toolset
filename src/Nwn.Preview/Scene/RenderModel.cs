// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Nwn.Preview.Scene
{
    /// <summary>All renderable geometry and optional placeable-preview metadata for one MDL.</summary>
    public sealed class RenderModel
    {
        public string Name { get; init; } = string.Empty;
        public IReadOnlyList<RenderMesh> Meshes { get; init; } = Array.Empty<RenderMesh>();
        public IReadOnlyList<RenderAnimation> Animations { get; init; } = Array.Empty<RenderAnimation>();
        public IReadOnlyList<RenderEmitter> Emitters { get; init; } = Array.Empty<RenderEmitter>();
        public string? DefaultAnimationName { get; init; }

        /// <summary>
        /// This model was built from an invisible transition door's editor-only geometry. Area
        /// viewports draw it flat and translucent instead of treating it as ordinary game artwork.
        /// </summary>
        public bool IsDoorTransitionGeometry { get; init; }

        /// <summary>Hidden selection surfaces of an invisible placeable, shown only in the editor.</summary>
        public bool IsInvisiblePlaceableGeometry { get; init; }

        /// <summary>
        /// The palette index each PLT layer is dyed with (skin, hair, metal, cloth, leather, tattoo),
        /// or empty for a model with no dyed textures.
        /// </summary>
        /// <remarks>
        /// Carried on the model rather than passed alongside it because a PLT is not a picture until
        /// its layers are coloured - a renderer that loads one without these gets the palette's
        /// default row, which is why the armor preview ignored every dye channel in the viewport
        /// while the 2D icon beside it honoured them.
        /// </remarks>
        public IReadOnlyDictionary<int, int> LayerColorIndices { get; init; } =
            new Dictionary<int, int>();

        /// <summary>
        /// The model's world-space bounding box at rest, or null when it has no drawable vertices.
        /// Uses each mesh's settled <see cref="RenderMesh.Transform"/> (the last idle frame), which
        /// is what the viewport draws when nothing is animating.
        /// </summary>
        public (Vector3 Minimum, Vector3 Maximum)? ComputeBounds()
        {
            var minimum = new Vector3(float.MaxValue);
            var maximum = new Vector3(float.MinValue);
            var found = false;

            foreach (var mesh in Meshes)
            {
                for (var vertex = 0; vertex < mesh.VertexCount; vertex++)
                {
                    var local = new Vector3(
                        mesh.Positions[vertex * 3],
                        mesh.Positions[vertex * 3 + 1],
                        mesh.Positions[vertex * 3 + 2]);
                    var world = Vector3.Transform(local, mesh.Transform);
                    if (!float.IsFinite(world.X) || !float.IsFinite(world.Y) || !float.IsFinite(world.Z))
                        continue;

                    minimum = Vector3.Min(minimum, world);
                    maximum = Vector3.Max(maximum, world);
                    found = true;
                }
            }

            return found ? (minimum, maximum) : null;
        }
    }
}

