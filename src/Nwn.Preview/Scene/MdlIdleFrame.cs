namespace Nwn.Preview.Scene;

public readonly record struct MdlIdleFrame(IReadOnlyDictionary<string, PosedNode> Pose, float Seconds);