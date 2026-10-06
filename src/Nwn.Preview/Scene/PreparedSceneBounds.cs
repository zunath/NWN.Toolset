using System.Numerics;

namespace Nwn.Preview.Scene;

/// <summary>Axis-aligned bounds of every prepared mesh vertex in model space.</summary>
public readonly record struct PreparedSceneBounds(Vector3 Minimum, Vector3 Maximum)
{
    public Vector3 Center => (Minimum + Maximum) * 0.5f;

    public float Radius => Math.Max(0.01f, Vector3.Distance(Minimum, Maximum) * 0.5f);
}