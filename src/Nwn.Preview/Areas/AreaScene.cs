// SPDX-License-Identifier: MIT

using System.Numerics;
using Nwn.Preview.Scene;

namespace Nwn.Preview.Areas
{
    /// <summary>
    /// A render-ready scene description for one area: its tile grid placements and placed-instance
    /// markers, plus assembly diagnostics. Pure data - no GL/app dependency; consumed by the
    /// area view.
    /// </summary>
    public sealed class AreaScene
    {
        public required string Tileset { get; init; }
        public required int Width { get; init; }
        public required int Height { get; init; }
        public required IReadOnlyList<TilePlacement> Tiles { get; init; }
        public required IReadOnlyList<InstanceMarker> Instances { get; init; }
        public required AreaSceneDiagnostics Diagnostics { get; init; }

        /// <summary>
        /// Whether this area's tileset declares <c>Interior=1</c> in its .set - i.e. its tiles are
        /// rooms with a ceiling over them rather than open sky.
        /// </summary>
        /// <remarks>
        /// This is what decides whether the viewport's hide-ceilings pass applies. Interior and
        /// exterior tilesets both mark overhead geometry with the MDL <c>tilefade</c> flag, but they
        /// mean different things by it: zsf01 flags the ceiling slabs sealing each room, which have to
        /// go or the area reads as a field of blank plates; ttw01 flags the forest canopy, which
        /// Aurora draws - a wood with its treetops removed is a field of bare poles, and the reference
        /// toolset shows the canopy. False for an area whose tileset would not resolve, which is also
        /// the safe answer: nothing is hidden.
        /// </remarks>
        public bool IsInteriorTileset { get; init; }

        /// <summary>
        /// Every doorway the placed tiles declare, in world space - the only positions a door may be
        /// hung at. Empty for an area whose tileset would not resolve, or one laid entirely with
        /// tiles that carry no door nodes.
        /// </summary>
        public IReadOnlyList<TileDoorAnchor> DoorAnchors { get; init; } = Array.Empty<TileDoorAnchor>();

        /// <summary>
        /// How near a ground point has to be for a door to reach a doorway, in metres.
        /// </summary>
        /// <remarks>
        /// Half a tile, so the doorways of the tile under the point are in reach and a doorway two rooms
        /// away is not. This was once unbounded, on the reasoning that a door has to go somewhere and
        /// "nothing happened" is a worse answer than "it went to the doorway you were nearest". It is
        /// not: with no limit the placement ghost teleports to whichever doorway is nearest anywhere in
        /// the area, so the preview is never under the cursor and the click lands somewhere the builder
        /// never pointed at. Bounded, the door rides the cursor and drops into a doorway when it reaches
        /// one, which is both what Aurora does and what can be seen happening.
        /// <para>
        /// Measured with <see cref="DoorAnchorHeightWeight"/> applied, so the doorway a storey up is far
        /// out of reach from the floor below rather than merely losing a tie-break to it.
        /// </para>
        /// </remarks>
        public const float DoorSnapRadius = AreaGrid.TileSize / 2f;

        /// <summary>
        /// How much a metre of height counts for against a metre across the floor when choosing a
        /// doorway.
        /// </summary>
        /// <remarks>
        /// Elevation has to count for something or a multi-storey area cannot be edited: doorways stack
        /// almost exactly above one another, so ignoring Z entirely meant a click upstairs took whichever
        /// lower-floor anchor happened to be marginally nearer in X/Y. It must not dominate either -
        /// weighting height equally would let a doorway on the same floor lose to a nearer one above it.
        /// A tile storey is 10m at this tileset's transition height, so a modest weight separates floors
        /// decisively while leaving same-floor choices to the floor distance that the builder is aiming
        /// with.
        /// </remarks>
        private const float DoorAnchorHeightWeight = 4f;

        /// <summary>
        /// How close a door has to be to a doorway to count as filling it, in metres.
        /// </summary>
        /// <remarks>
        /// Deliberately tight. A door hung in a doorway sits exactly on the node, so the only thing this
        /// absorbs is float drift through the tile transform; anything further off is a door standing
        /// elsewhere on the same tile - the corpus has those - and must not blank out a doorway that is
        /// really empty. Height is weighted in as it is for the snap, so the door in the doorway directly
        /// overhead does not fill the one below it.
        /// </remarks>
        public const float DoorwayFilledRadius = 0.25f;

        /// <summary>
        /// Whether a door already hangs in <paramref name="anchor"/>. <paramref name="ignore"/> excludes
        /// one instance from the answer, which is how a door being dragged does not count as filling the
        /// doorway it is being dragged out of.
        /// </summary>
        /// <remarks>
        /// A doorway holds one door. The tile owns the gap in the wall and the walkmesh cut through it,
        /// so a second leaf in the same frame is not a second door - it is two doors occupying one
        /// another, which the engine draws as z-fighting and the builder cannot select apart.
        /// </remarks>
        public bool IsDoorwayFilled(TileDoorAnchor anchor, InstanceMarker? ignore = null)
        {
            ArgumentNullException.ThrowIfNull(anchor);

            foreach (var instance in Instances)
            {
                if (instance.Kind != InstanceMarkerKind.Door || ReferenceEquals(instance, ignore))
                    continue;

                if (DoorAnchorDistanceSquared(instance.Position, anchor.Position)
                    <= DoorwayFilledRadius * DoorwayFilledRadius)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Whether this area has a doorway that no door is hanging in yet.</summary>
        public bool HasEmptyDoorway()
        {
            foreach (var anchor in DoorAnchors)
            {
                if (!IsDoorwayFilled(anchor))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// The empty doorway within <see cref="DoorSnapRadius"/> of a point, nearest first and preferring
        /// the same storey, or null when the point is near none, the ones in reach are already filled, or
        /// this area declares no doorways at all. <paramref name="ignore"/> is the door being moved, which
        /// must not be treated as filling the doorway it currently sits in.
        /// </summary>
        /// <remarks>
        /// Lives here rather than in the viewport because two paths need the same answer - the placement
        /// ghost, and moving a door that is already placed, which without it could detach a door from its
        /// tile frame and walkmesh opening.
        /// </remarks>
        public TileDoorAnchor? NearestEmptyDoorway(Vector3 groundPoint, InstanceMarker? ignore = null)
        {
            TileDoorAnchor? best = null;
            var bestDistance = DoorSnapRadius * DoorSnapRadius;

            foreach (var anchor in DoorAnchors)
            {
                var distance = DoorAnchorDistanceSquared(anchor.Position, groundPoint);
                if (distance > bestDistance)
                    continue;

                if (IsDoorwayFilled(anchor, ignore))
                    continue;

                bestDistance = distance;
                best = anchor;
            }

            return best;
        }

        /// <summary>
        /// How far apart two points are for the purpose of choosing a doorway: across the floor, plus
        /// height counted at <see cref="DoorAnchorHeightWeight"/>. Squared, since every caller compares.
        /// </summary>
        private static float DoorAnchorDistanceSquared(Vector3 a, Vector3 b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            var dz = (a.Z - b.Z) * DoorAnchorHeightWeight;
            return dx * dx + dy * dy + dz * dz;
        }

        /// <summary>
        /// This scene with <paramref name="existing"/> swapped for <paramref name="replacement"/>, or
        /// null when that marker is not in this scene (a stale selection from a superseded build).
        /// </summary>
        /// <remarks>
        /// The tile list is carried across <b>by reference</b>, not copied - that is what lets the
        /// renderer notice the grid did not change and keep its uploaded tile batches and walkmesh
        /// buffers rather than rebuilding them to move one object.
        /// </remarks>
        public AreaScene? WithInstanceReplaced(InstanceMarker existing, InstanceMarker replacement)
        {
            var index = -1;
            for (var i = 0; i < Instances.Count; i++)
            {
                if (ReferenceEquals(Instances[i], existing))
                {
                    index = i;
                    break;
                }
            }

            if (index < 0)
                return null;

            var instances = Instances.ToArray();
            instances[index] = replacement;

            return WithInstances(instances);
        }

        /// <summary>
        /// This scene with one newly placed instance added, leaving every area-wide render input shared.
        /// </summary>
        /// <remarks>
        /// Full scene assembly groups markers by kind. Insert into that same ordering so the cheap
        /// placement path produces the same list shape as the host area assembler, while
        /// preserving the tile list by reference so the renderer does not rebuild its tile batches.
        /// </remarks>
        public AreaScene WithInstanceAdded(InstanceMarker instance)
        {
            ArgumentNullException.ThrowIfNull(instance);

            var insertAt = Instances.Count;
            for (var i = 0; i < Instances.Count; i++)
            {
                if (Instances[i].Kind > instance.Kind)
                {
                    insertAt = i;
                    break;
                }
            }

            var instances = new InstanceMarker[Instances.Count + 1];
            for (var i = 0; i < insertAt; i++)
                instances[i] = Instances[i];
            instances[insertAt] = instance;
            for (var i = insertAt; i < Instances.Count; i++)
                instances[i + 1] = Instances[i];

            return WithInstances(instances);
        }

        private AreaScene WithInstances(IReadOnlyList<InstanceMarker> instances)
        {
            return new AreaScene
            {
                Tileset = Tileset,
                Width = Width,
                Height = Height,
                Tiles = Tiles,
                Instances = instances,
                Diagnostics = Diagnostics,
                DoorAnchors = DoorAnchors,
                IsInteriorTileset = IsInteriorTileset,
                Lighting = Lighting
            };
        }

        /// <summary>
        /// The area's decoded ambient/diffuse lighting, from its .are sun colors by day or
        /// moon colors at night. Defaults to a neutral mid-gray when not set (e.g. synthetic test
        /// scenes) so existing callers that build an <see cref="AreaScene"/> directly still compile.
        /// </summary>
        public AreaLighting Lighting { get; init; } = AreaLighting.Default;
    }
}



