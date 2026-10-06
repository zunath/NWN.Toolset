// SPDX-License-Identifier: MIT

using System.Numerics;
using Nwn.Preview.Scene;

namespace Nwn.Preview.Areas;

/// <summary>Host-resolved appearance data used when composing one area instance marker.</summary>
public sealed record ResolvedInstanceAppearance
{
    public RenderModel? Model { get; init; }
    public Matrix4x4? ModelCorrection { get; init; }
    public bool IsDoorTransition { get; init; }
    public IReadOnlyDictionary<string, int> TintMapOverrides { get; init; }
        = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
}
