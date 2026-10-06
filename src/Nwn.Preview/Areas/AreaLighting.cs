// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Nwn.Preview.Areas
{
    /// <summary>
    /// True per-area lighting decoded from the .are: the sun ambient/diffuse colors during
    /// the day, the moon colors at night. Colors are linear RGB in 0..1. This is the faithful area
    /// color - a presentation layer (the GL area view) may brighten it for editor visibility, since
    /// authored night colors are near-black.
    /// </summary>
    public sealed class AreaLighting
    {
        public required Vector3 AmbientColor { get; init; }
        public required Vector3 DiffuseColor { get; init; }
        public required bool IsNight { get; init; }

        /// <summary>The area's fog colour, sun or moon to match <see cref="IsNight"/>.</summary>
        public Vector3 FogColor { get; init; }

        /// <summary>
        /// Fog thickness as a per-metre extinction coefficient, converted from the .are's 0-15
        /// FogAmount. Zero means the area authored no fog.
        /// </summary>
        public float FogDensity { get; init; }

        /// <summary>Turns the .are's 0-15 FogAmount into a per-metre extinction coefficient.</summary>
        /// <remarks>
        /// At the top of the range this puts roughly half the light through at 25m, which is dense
        /// enough to read as weather without hiding the far side of a normal interior.
        /// </remarks>
        public static float DecodeFogDensity(int fogAmount) =>
            Math.Clamp(fogAmount, 0, 15) * 0.0018f;

        /// <summary>Neutral mid-gray fallback for scenes/areas that carry no lighting fields.</summary>
        public static AreaLighting Default { get; } = new()
        {
            AmbientColor = new Vector3(0.5f, 0.5f, 0.5f),
            DiffuseColor = new Vector3(0.5f, 0.5f, 0.5f),
            IsNight = false
        };

        /// <summary>
        /// Decodes an NWN packed area color (0x00BBGGRR: red = low byte, green = middle, blue =
        /// high) into a linear 0..1 RGB vector.
        /// </summary>
        public static Vector3 DecodeColor(uint packed) => new(
            (packed & 0xFF) / 255f,
            ((packed >> 8) & 0xFF) / 255f,
            ((packed >> 16) & 0xFF) / 255f);
    }
}

