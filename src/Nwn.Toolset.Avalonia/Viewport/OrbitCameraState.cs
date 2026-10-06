using System.Numerics;

namespace Nwn.Toolset.Avalonia.Viewport;

/// <summary>Bounded orbit, pan, and zoom state for the shared model viewport.</summary>
public sealed class OrbitCameraState
{
    private const float MinimumDistance = 0.05f;
    private const float MaximumDistance = 100_000f;
    private const float PitchLimit = 1.553343f;

    public Vector3 Target { get; private set; }
    public float Yaw { get; private set; } = 0.7f;
    public float Pitch { get; private set; } = 0.3f;
    public float Distance { get; private set; } = 2.5f;

    public void Fit(Vector3 center, float radius)
    {
        if (!IsFinite(center) || !float.IsFinite(radius) || radius <= 0)
            throw new ArgumentOutOfRangeException(nameof(radius), "Camera bounds must be finite and have positive radius.");
        Target = center;
        Distance = Math.Clamp(radius * 2.8f, MinimumDistance, MaximumDistance);
    }

    public void Orbit(float deltaX, float deltaY)
    {
        if (!float.IsFinite(deltaX) || !float.IsFinite(deltaY))
            return;
        Yaw = Wrap(Yaw + deltaX * 0.008f);
        Pitch = Math.Clamp(Pitch + deltaY * 0.008f, -PitchLimit, PitchLimit);
    }

    public void Pan(float deltaX, float deltaY)
    {
        if (!float.IsFinite(deltaX) || !float.IsFinite(deltaY))
            return;
        var direction = Direction();
        var right = Vector3.Normalize(Vector3.Cross(direction, Vector3.UnitZ));
        if (right.LengthSquared() < 0.5f)
            right = Vector3.UnitY;
        var up = Vector3.Normalize(Vector3.Cross(right, direction));
        Target += (-right * deltaX + up * deltaY) * Distance * 0.0018f;
    }

    public void Zoom(float wheelDelta)
    {
        if (!float.IsFinite(wheelDelta))
            return;
        Distance = Math.Clamp(Distance * MathF.Exp(-wheelDelta * 0.12f), MinimumDistance, MaximumDistance);
    }

    public Vector3 Position => Target + Direction() * Distance;

    private Vector3 Direction() => new(
        MathF.Cos(Pitch) * MathF.Cos(Yaw),
        MathF.Cos(Pitch) * MathF.Sin(Yaw),
        MathF.Sin(Pitch));

    private static float Wrap(float angle)
    {
        var wrapped = angle % (MathF.Tau);
        return wrapped < 0 ? wrapped + MathF.Tau : wrapped;
    }

    private static bool IsFinite(Vector3 vector) =>
        float.IsFinite(vector.X) && float.IsFinite(vector.Y) && float.IsFinite(vector.Z);
}
