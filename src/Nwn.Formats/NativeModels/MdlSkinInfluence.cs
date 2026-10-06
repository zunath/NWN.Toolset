// SPDX-License-Identifier: MIT

namespace Nwn.Formats.NativeModels;

/// <summary>
/// A named bone influence declared by an ASCII skinmesh vertex.
/// </summary>
public readonly record struct MdlSkinInfluence(string BoneName, float Weight);
