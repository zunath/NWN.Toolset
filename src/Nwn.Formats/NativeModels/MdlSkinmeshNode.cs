// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Nwn.Formats.NativeModels;

public sealed class MdlSkinmeshNode : MdlTrimeshNode
{
    public MdlSkinInfluence[][] VertexInfluences { get; set; } = Array.Empty<MdlSkinInfluence[]>();

    public Vector4[] BoneWeights { get; set; } = Array.Empty<Vector4>();

    public MdlBoneIndices[] BoneIndices { get; set; } = Array.Empty<MdlBoneIndices>();

    public short[] BoneMapping { get; set; } = Array.Empty<short>();

    public Quaternion[] BoneQuaternions { get; set; } = Array.Empty<Quaternion>();

    public Vector3[] BoneTranslations { get; set; } = Array.Empty<Vector3>();
}
