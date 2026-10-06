using System.Numerics;

namespace Nwn.Preview.Scene;

/// <summary>One immutable model-space scene placed in an assembled scene.</summary>
public sealed record SceneInstance(string Id, PreparedScene Scene, Matrix4x4 Transform);
