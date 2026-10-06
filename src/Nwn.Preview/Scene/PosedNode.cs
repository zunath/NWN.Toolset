namespace Nwn.Preview.Scene;

using System.Numerics;

public readonly record struct PosedNode(Vector3 Position, Quaternion Orientation, float Scale);