// SPDX-License-Identifier: MIT

using System.Numerics;
using Nwn.Preview.Scene;

namespace Nwn.Preview.Areas
{
    /// <summary>
    /// A lightweight placement marker for one instance from a .git list. Deliberately does not
    /// resolve the instance's appearance model - that is the GL area renderer's concern
    /// view exists; this is data assembly only.
    /// </summary>
    public sealed class InstanceMarker
    {
        public required InstanceMarkerKind Kind { get; init; }

        /// <summary>Zero-based ordinal in the matching GIT list; negative only for standalone markers.</summary>
        public int ListIndex { get; init; } = -1;

        /// <summary>The blueprint resref this instance was placed from ("TemplateResRef", or "ResRef" for stores).</summary>
        public string? TemplateResRef { get; init; }

        public string? Tag { get; init; }

        /// <summary>
        /// Dye indices for this instance's PLT layers, overriding whatever the model carries. Empty
        /// for everything undyed.
        /// </summary>
        /// <remarks>
        /// Here as well as on the model so that changing a dye does not have to produce new geometry:
        /// re-resolving the model meant re-reading 19 part files, re-composing them and re-uploading
        /// every vertex buffer on each click of a colour swatch, which is most of what made picking a
        /// colour feel slow. The geometry is the same body; only its colours changed.
        /// </remarks>
        public IReadOnlyDictionary<int, int> LayerColorIndices { get; init; } =
            new Dictionary<int, int>();

        public required Vector3 Position { get; init; }

        /// <summary>Heading as a (cos, sin) unit vector - XOrientation/YOrientation, or Bearing converted for placeables/doors. (1,0) when the instance carries no heading (ambient sounds).</summary>
        public required Vector2 Orientation { get; init; }

        /// <summary>
        /// Optional enhanced-edition model transform (scale, Euler rotation, translation) in
        /// instance-local space. Composed before <see cref="Orientation"/> and
        /// <see cref="Position"/> for rendering and picking.
        /// </summary>
        public Matrix4x4 VisualTransform { get; init; } = Matrix4x4.Identity;

        /// <summary>Polygon points (world-space) for a trigger's volume; null for every other kind or when absent.</summary>
        public IReadOnlyList<Vector3>? Geometry { get; init; }

        /// <summary>
        /// Resolved render geometry for kinds whose appearance lives on the instance itself
        /// (placeables via placeables.2da ModelName, doors via doortypes.2da Model), shared
        /// through the model cache. Null when unresolvable or when appearance services weren't
        /// supplied — the renderer draws the kind marker instead.
        /// </summary>
        public RenderModel? Model { get; init; }

        /// <summary>
        /// This door's 2DA row declares <c>VisibleModel=0</c>, making it an invisible runtime area
        /// transition that the toolset must represent with translucent editor geometry.
        /// </summary>
        public bool IsDoorTransition { get; init; }

        /// <summary>
        /// Stored TM_* local ints for tint-map materials. RGB marker values and legacy palette
        /// values are both retained so the renderer can mirror the game fallback behavior.
        /// </summary>
        public IReadOnlyDictionary<string, int> TintMapOverrides { get; init; } =
            new Dictionary<string, int>(StringComparer.Ordinal);

        /// <summary>
        /// A sound instance's MinDistance in metres - the range it plays at full volume, which
        /// Aurora draws as the dotted sphere around the marker. Null for every other kind and for
        /// sounds that carry no such field.
        /// </summary>
        public float? SoundMinDistance { get; init; }

        /// <summary>
        /// A sound instance's MaxDistance in metres - the range it is audible at all, which Aurora
        /// draws as the large circle around the marker. Null for every other kind and for sounds
        /// that carry no such field.
        /// </summary>
        public float? SoundMaxDistance { get; init; }

        /// <summary>
        /// Whether a sound instance is positional (Positional=1). Area-wide sounds get no distance
        /// rings - range means nothing for a sound that plays everywhere. Always false for other kinds.
        /// </summary>
        public bool IsPositionalSound { get; init; }

        /// <summary>
        /// This marker moved and/or turned, with everything else - kind, tag, resolved model, EE
        /// visual transform - carried across unchanged.
        /// </summary>
        /// <remarks>
        /// Exists so a drag or a rotate can update the scene in place instead of reparsing both
        /// documents and rebuilding every tile and instance to move one object. Nothing about a
        /// placement's own geometry depends on where a different instance sits, so a transform is the
        /// one edit that can be applied this cheaply.
        /// <para>
        /// <see cref="Geometry"/> is world-space here but authored as offsets from the instance's
        /// position, so it travels with a move. It does <b>not</b> turn with
        /// <see cref="Orientation"/> - a trigger's volume is stored unrotated, and rotating one in
        /// the toolset leaves its polygon where it was, which is what the engine does too.
        /// </para>
        /// </remarks>
        public InstanceMarker WithTransform(Vector3 position, Vector2 orientation)
        {
            var delta = position - Position;
            var geometry = Geometry;
            if (geometry != null && delta != Vector3.Zero)
            {
                var moved = new Vector3[geometry.Count];
                for (var i = 0; i < geometry.Count; i++)
                    moved[i] = geometry[i] + delta;
                geometry = moved;
            }

            return new InstanceMarker
            {
                Kind = Kind,
                ListIndex = ListIndex,
                TemplateResRef = TemplateResRef,
                Tag = Tag,
                Position = position,
                Orientation = orientation,
                VisualTransform = VisualTransform,
                Geometry = geometry,
                Model = Model,
                IsDoorTransition = IsDoorTransition,
                LayerColorIndices = LayerColorIndices,
                TintMapOverrides = TintMapOverrides,
                SoundMinDistance = SoundMinDistance,
                SoundMaxDistance = SoundMaxDistance,
                IsPositionalSound = IsPositionalSound
            };
        }
    }
}

