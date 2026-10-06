// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Nwn.Preview.Scene
{
    /// <summary>Bounded emitter metadata used by the placeable preview's particle cue.</summary>
    public sealed class RenderEmitter
    {
        public string NodeName { get; init; } = string.Empty;
        public string TextureName { get; init; } = string.Empty;
        public Matrix4x4 Transform { get; init; } = Matrix4x4.Identity;
        public IReadOnlyDictionary<string, IReadOnlyList<Matrix4x4>> AnimationFrames { get; init; } =
            new Dictionary<string, IReadOnlyList<Matrix4x4>>(StringComparer.OrdinalIgnoreCase);
        public int XGrid { get; init; } = 1;
        public int YGrid { get; init; } = 1;
        public string Update { get; init; } = string.Empty;
        public string RenderMode { get; init; } = string.Empty;
        public string Blend { get; init; } = string.Empty;
        public string Chunk { get; init; } = string.Empty;
        public bool TextureIsTwoSided { get; init; }
        public bool Loop { get; init; }
        public ushort RenderOrder { get; init; }
        public float DeadSpace { get; init; }
        public float BlastRadius { get; init; }
        public float BlastLength { get; init; }
    }
}

