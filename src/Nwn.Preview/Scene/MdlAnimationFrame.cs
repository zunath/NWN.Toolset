namespace Nwn.Preview.Scene;

public readonly record struct MdlAnimationFrame(IReadOnlyDictionary<string, PosedNode> Pose, float Seconds);