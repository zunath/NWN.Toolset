// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Nwn.Preview.Areas
{
    /// <summary>
    /// A world-space ray (origin + normalized direction) produced by unprojecting a screen point,
    /// consumed by <see cref="AreaPicking"/> for instance hit-testing.
    /// </summary>
    public readonly struct PickRay
    {
        public PickRay(Vector3 origin, Vector3 direction)
        {
            Origin = origin;
            Direction = direction;
        }

        public Vector3 Origin { get; }

        /// <summary>Normalized (unit-length) ray direction, except in the degenerate near==far case, where it falls back to +X.</summary>
        public Vector3 Direction { get; }
    }
}

