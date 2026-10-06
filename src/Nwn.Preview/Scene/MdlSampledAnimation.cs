namespace Nwn.Preview.Scene;

public readonly record struct MdlSampledAnimation(string Name, float Length, IReadOnlyList<IReadOnlyDictionary<string, PosedNode>> Frames);